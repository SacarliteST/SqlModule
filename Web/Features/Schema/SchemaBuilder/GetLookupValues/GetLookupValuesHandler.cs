using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Data.Core;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.SchemaBuilder.GetLookupValues;

internal sealed record GetLookupValuesQuery(
    Guid TargetDbId,
    Guid TableId,
    Guid ValueColumnId,
    Guid? LabelColumnId,
    string? Search,
    int Offset,
    int Limit) : IRequest<Result<LookupValuesResponse>>;

internal sealed class GetLookupValuesHandler(AppDbContext db)
    : IRequestHandler<GetLookupValuesQuery, Result<LookupValuesResponse>>
{
    public async Task<Result<LookupValuesResponse>> Handle(GetLookupValuesQuery query, CancellationToken ct)
    {
        if (!await db.TargetDbs.AsNoTracking().AnyAsync(target => target.Id == query.TargetDbId, ct))
        {
            return Result<LookupValuesResponse>.Fail(SchemaAggregateErrors.NotFound(query.TargetDbId));
        }

        if (!await db.MetaTables.AsNoTracking()
                .AnyAsync(table => table.Id == query.TableId && table.TargetDbId == query.TargetDbId, ct))
        {
            return Result<LookupValuesResponse>.Fail(Error.NotFound("MetaTable", query.TableId));
        }

        var columnIds = query.LabelColumnId.HasValue
            ? new[] { query.ValueColumnId, query.LabelColumnId.Value }
            : new[] { query.ValueColumnId };
        var columns = await db.MetaAttributes.AsNoTracking()
            .Where(column => columnIds.Contains(column.Id))
            .Select(column => new { column.Id, column.MetaTableId })
            .ToDictionaryAsync(column => column.Id, ct);
        foreach (var columnId in columnIds)
        {
            if (!columns.TryGetValue(columnId, out var column))
            {
                return Result<LookupValuesResponse>.Fail(LookupValuesErrors.ColumnNotFound(columnId));
            }

            if (column.MetaTableId != query.TableId)
            {
                return Result<LookupValuesResponse>.Fail(
                    LookupValuesErrors.ColumnDoesNotBelongToTable(columnId, query.TableId));
            }
        }

        IQueryable<LookupRow> rows;
        if (query.LabelColumnId.HasValue)
        {
            rows =
                from record in db.DataRecords.AsNoTracking()
                join valueCell in db.CellValues.AsNoTracking()
                    on record.Id equals valueCell.DataRecordId
                join labelCell in db.CellValues.AsNoTracking()
                        .Where(cell => cell.MetaAttributeId == query.LabelColumnId.Value)
                    on record.Id equals labelCell.DataRecordId into labelCells
                from labelCell in labelCells.DefaultIfEmpty()
                where record.MetaTableId == query.TableId &&
                      valueCell.MetaAttributeId == query.ValueColumnId &&
                      valueCell.TextValue != null
                select new LookupRow
                {
                    RowId = record.Id,
                    SortOrder = record.SortOrder,
                    Value = valueCell.TextValue,
                    LabelValue = labelCell.TextValue
                };
        }
        else
        {
            rows =
                from record in db.DataRecords.AsNoTracking()
                join valueCell in db.CellValues.AsNoTracking()
                    on record.Id equals valueCell.DataRecordId
                where record.MetaTableId == query.TableId &&
                      valueCell.MetaAttributeId == query.ValueColumnId &&
                      valueCell.TextValue != null
                select new LookupRow
                {
                    RowId = record.Id,
                    SortOrder = record.SortOrder,
                    Value = valueCell.TextValue,
                    LabelValue = null
                };
        }

        if (!String.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{EscapeLikePattern(query.Search)}%";
            rows = rows.Where(row =>
                EF.Functions.ILike(row.Value!, pattern, "\\") ||
                row.LabelValue != null && EF.Functions.ILike(row.LabelValue, pattern, "\\"));
        }

        var count = await rows.CountAsync(ct);
        var page = await rows
            .OrderBy(row => row.SortOrder)
            .ThenBy(row => row.RowId)
            .Skip(query.Offset)
            .Take(query.Limit)
            .ToListAsync(ct);

        return new LookupValuesResponse
        {
            Count = count,
            Offset = query.Offset,
            Limit = query.Limit,
            Items = page.Select(row => new LookupValueItemResponse
            {
                RowId = row.RowId,
                Value = row.Value!,
                Label = query.LabelColumnId.HasValue && !String.IsNullOrEmpty(row.LabelValue)
                    ? $"{row.Value} — {row.LabelValue}"
                    : row.Value!
            }).ToList()
        };
    }

    private static string EscapeLikePattern(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);

    private sealed class LookupRow
    {
        public required Guid RowId { get; init; }
        public required int? SortOrder { get; init; }
        public required string? Value { get; init; }
        public required string? LabelValue { get; init; }
    }
}
