using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SQLModule.Domain.Training;
using SQLModule.Sandbox;

namespace SQLModule.Web.Features.Training.Attempts;

internal sealed record AttemptResultSnapshot(
    AttemptResultSnapshotState State,
    IReadOnlyList<string>? Columns,
    IReadOnlyList<IReadOnlyList<string?>>? Rows,
    int? ReturnedRowCount,
    bool IsTruncated,
    int? RowLimit,
    DateTimeOffset? CreatedAt,
    DateTimeOffset? ExpiresAt,
    string? ColumnsJson = null,
    string? RowsJson = null);

internal interface IAttemptResultSnapshotService
{
    int GetEffectiveRowLimit(int sandboxMaxRows);
    AttemptResultSnapshot Create(QueryResultSet result, int rowLimit, DateTimeOffset now);
    AttemptResultSnapshot Read(Attempt attempt, DateTimeOffset now);
    void Apply(Attempt attempt, AttemptResultSnapshot snapshot);
}

internal sealed class AttemptResultSnapshotService(IOptions<AttemptResultSnapshotsOptions> options)
    : IAttemptResultSnapshotService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AttemptResultSnapshotsOptions settings = options.Value;

    public int GetEffectiveRowLimit(int sandboxMaxRows)
        => Math.Max(1, Math.Min(sandboxMaxRows, Math.Max(1, settings.MaxRows)));

    public AttemptResultSnapshot Create(QueryResultSet result, int rowLimit, DateTimeOffset now)
    {
        var expiresAt = now.AddDays(Math.Max(1, settings.RetentionDays));
        if (!result.Succeeded)
        {
            return new AttemptResultSnapshot(
                AttemptResultSnapshotState.NotProduced,
                null, null, null, false, rowLimit, now, expiresAt);
        }

        var maxColumns = Math.Max(1, settings.MaxColumns);
        var maxCellLength = Math.Max(1, settings.MaxCellLength);
        var columns = result.Columns.Take(maxColumns)
            .Select(value => Limit(value, maxCellLength))
            .ToList();
        var rows = result.Rows.Take(rowLimit)
            .Select(row => (IReadOnlyList<string?>)row.Take(columns.Count)
                .Select(value => value is null ? null : Limit(value, maxCellLength))
                .ToList())
            .ToList();
        var truncated = result.IsTruncated || result.Rows.Count > rowLimit || result.Columns.Count > maxColumns;

        var columnsJson = JsonSerializer.Serialize(columns, JsonOptions);
        var rowsJson = JsonSerializer.Serialize(rows, JsonOptions);
        var maxBytes = Math.Max(4, settings.MaxSerializedBytes);
        while (SerializedSize(columnsJson, rowsJson) > maxBytes && rows.Count > 0)
        {
            rows.RemoveAt(rows.Count - 1);
            rowsJson = JsonSerializer.Serialize(rows, JsonOptions);
            truncated = true;
        }

        while (SerializedSize(columnsJson, rowsJson) > maxBytes && columns.Count > 0)
        {
            columns.RemoveAt(columns.Count - 1);
            columnsJson = JsonSerializer.Serialize(columns, JsonOptions);
            truncated = true;
        }

        return new AttemptResultSnapshot(
            AttemptResultSnapshotState.Available,
            columns, rows, rows.Count, truncated, rowLimit, now, expiresAt,
            columnsJson, rowsJson);
    }

    public AttemptResultSnapshot Read(Attempt attempt, DateTimeOffset now)
    {
        if (attempt.ResultSnapshotState == AttemptResultSnapshotState.Available &&
            attempt.ResultSnapshotExpiresAt.HasValue &&
            attempt.ResultSnapshotExpiresAt.Value <= now)
        {
            return new AttemptResultSnapshot(
                AttemptResultSnapshotState.Expired,
                null, null, null, false, attempt.ResultRowLimit,
                attempt.ResultSnapshotCreatedAt, attempt.ResultSnapshotExpiresAt);
        }

        if (attempt.ResultSnapshotState != AttemptResultSnapshotState.Available)
        {
            return new AttemptResultSnapshot(
                attempt.ResultSnapshotState,
                null, null, null, false, attempt.ResultRowLimit,
                attempt.ResultSnapshotCreatedAt, attempt.ResultSnapshotExpiresAt);
        }

        var columns = attempt.ActualColumnsJson is null
            ? null
            : JsonSerializer.Deserialize<List<string>>(attempt.ActualColumnsJson, JsonOptions);
        var rows = attempt.ActualRowsJson is null
            ? null
            : JsonSerializer.Deserialize<List<List<string?>>>(attempt.ActualRowsJson, JsonOptions)?
                .Select(row => (IReadOnlyList<string?>)row).ToList();

        return new AttemptResultSnapshot(
            AttemptResultSnapshotState.Available,
            columns, rows, rows?.Count, attempt.IsResultTruncated, attempt.ResultRowLimit,
            attempt.ResultSnapshotCreatedAt, attempt.ResultSnapshotExpiresAt,
            attempt.ActualColumnsJson, attempt.ActualRowsJson);
    }

    public void Apply(Attempt attempt, AttemptResultSnapshot snapshot)
    {
        if (snapshot.State == AttemptResultSnapshotState.Available)
        {
            attempt.StoreResultSnapshot(
                snapshot.ColumnsJson!, snapshot.RowsJson!, snapshot.ReturnedRowCount!.Value,
                snapshot.IsTruncated, snapshot.RowLimit!.Value,
                snapshot.CreatedAt!.Value, snapshot.ExpiresAt!.Value);
            return;
        }

        attempt.MarkResultNotProduced(
            snapshot.RowLimit!.Value, snapshot.CreatedAt!.Value, snapshot.ExpiresAt!.Value);
    }

    private static string Limit(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];

    private static int SerializedSize(string columnsJson, string rowsJson)
        => Encoding.UTF8.GetByteCount(columnsJson) + Encoding.UTF8.GetByteCount(rowsJson);
}
