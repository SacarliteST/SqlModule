using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Data.Core;
using SQLModule.Domain.Schema;
using SQLModule.Web.Common.Cqrs;
using SQLModule.Web.Common.Sandbox;

namespace SQLModule.Web.Features.Schema.SchemaBuilder.BatchTableRows;

internal sealed record BatchTableRowsCommand(
    Guid TargetDbId,
    Guid TableId,
    string IdempotencyKey,
    BatchTableRowsRequest Request) : IRequest<Result<BatchTableRowsResponse>>;

internal sealed class BatchTableRowsHandler(AppDbContext db, ITargetDbDataValidator dataValidator)
    : IRequestHandler<BatchTableRowsCommand, Result<BatchTableRowsResponse>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<Result<BatchTableRowsResponse>> Handle(BatchTableRowsCommand command, CancellationToken ct)
    {
        var scope = $"rows:{command.TargetDbId}:{command.TableId}";
        var payloadJson = JsonSerializer.Serialize(command.Request, JsonOptions);
        var payloadHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payloadJson)));
        var receipt = await db.MutationReceipts.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Scope == scope && x.IdempotencyKey == command.IdempotencyKey, ct);
        if (receipt is not null)
        {
            return receipt.PayloadHash == payloadHash
                ? JsonSerializer.Deserialize<BatchTableRowsResponse>(receipt.ResponseJson, JsonOptions)!
                : Result<BatchTableRowsResponse>.Fail(Error.Conflict(
                    "IdempotencyKeyPayloadMismatch", "Idempotency-Key уже использован с другим запросом."));
        }

        var target = await db.TargetDbs.SingleOrDefaultAsync(x => x.Id == command.TargetDbId, ct);
        if (target is null)
        {
            return Result<BatchTableRowsResponse>.Fail(SchemaAggregateErrors.NotFound(command.TargetDbId));
        }

        if (target.IsReadOnly)
        {
            return Result<BatchTableRowsResponse>.Fail(SchemaAggregateErrors.EditingForbidden(command.TargetDbId));
        }

        var actualSchemaVersion = target.SchemaVersion.ToString(CultureInfo.InvariantCulture);
        if (command.Request.SchemaVersion != actualSchemaVersion)
        {
            return Result<BatchTableRowsResponse>.Fail(
                SchemaAggregateErrors.VersionConflict(command.Request.SchemaVersion ?? "<missing>", actualSchemaVersion));
        }

        var tableExists = await db.MetaTables.AnyAsync(
            x => x.Id == command.TableId && x.TargetDbId == command.TargetDbId, ct);
        if (!tableExists)
        {
            return Result<BatchTableRowsResponse>.Fail(Error.NotFound("MetaTable", command.TableId));
        }

        var columns = await db.MetaAttributes
            .Include(x => x.PhysicalType)
            .Where(x => x.MetaTableId == command.TableId)
            .ToDictionaryAsync(x => x.Id, ct);
        var requestedIds = command.Request.Changes!
            .Where(x => x.Id.HasValue).Select(x => x.Id!.Value).Distinct().ToList();
        var records = await db.DataRecords
            .Include(x => x.CellValues)
            .Where(x => requestedIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, ct);
        var createdIds = new Dictionary<string, Guid>(StringComparer.Ordinal);
        var changedIds = new List<Guid>();

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        foreach (var change in command.Request.Changes!)
        {
            var validationError = ValidateChange(change, command.TableId, columns, records);
            if (validationError is not null)
            {
                return Result<BatchTableRowsResponse>.Fail(validationError);
            }

            switch (change.Operation!.Value)
            {
                case TableRowOperation.Create:
                    {
                        var record = DataRecord.Create(command.TableId, change.SortOrder);
                        db.DataRecords.Add(record);
                        ApplyCells(record, change.Cells!, columns, db);
                        createdIds[change.TempId!] = record.Id;
                        changedIds.Add(record.Id);
                        break;
                    }
                case TableRowOperation.Update:
                    {
                        var record = records[change.Id!.Value];
                        record.Update(change.SortOrder);
                        ApplyCells(record, change.Cells!, columns, db);
                        changedIds.Add(record.Id);
                        break;
                    }
                case TableRowOperation.Delete:
                    db.DataRecords.Remove(records[change.Id!.Value]);
                    break;
            }
        }

        await db.SaveChangesAsync(ct);
        var physicalValidation = await dataValidator.ValidateAsync(command.TargetDbId, ct);
        if (!physicalValidation.IsSuccess)
        {
            return Result<BatchTableRowsResponse>.Fail(physicalValidation.Error!);
        }

        var response = await BuildResponse(actualSchemaVersion, changedIds, createdIds, columns, ct);
        db.MutationReceipts.Add(MutationReceipt.Create(
            scope, command.IdempotencyKey, payloadHash, JsonSerializer.Serialize(response, JsonOptions)));
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return response;
    }

    private static Error? ValidateChange(
        TableRowChange change,
        Guid tableId,
        IReadOnlyDictionary<Guid, MetaAttribute> columns,
        IReadOnlyDictionary<Guid, DataRecord> records)
    {
        if (change.Id.HasValue && (!records.TryGetValue(change.Id.Value, out var foundRecord) ||
                                   foundRecord.MetaTableId != tableId))
        {
            return Error.NotFound("DataRecord", change.Id.Value);
        }

        if (change.Id.HasValue)
        {
            var record = records[change.Id.Value];
            var actualVersion = record.UpdatedAt.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);
            if (change.Version != actualVersion)
            {
                return Error.PreconditionFailed("RowVersionConflict", $"Строка '{record.Id}' была изменена другим редактором.");
            }
        }

        if (change.Cells is null)
        {
            return null;
        }

        foreach (var (columnId, cell) in change.Cells)
        {
            if (!columns.TryGetValue(columnId, out var column))
            {
                return Error.Validation("CellValueInvalid", $"Колонка '{columnId}' не принадлежит таблице.");
            }

            if (column.IsRequired && cell.IsNull == true)
            {
                return Error.Validation("CellValueInvalid", $"Колонка '{column.AttributeName}' обязательна.");
            }

            if (cell.IsNull == false && !IsTransportValueValid(column.PhysicalType.TypeName, cell.Value))
            {
                return Error.Validation("CellValueInvalid", $"Значение колонки '{column.AttributeName}' не соответствует типу '{column.PhysicalType.TypeName}'.");
            }
        }

        if (change.Operation == TableRowOperation.Create)
        {
            foreach (var required in columns.Values.Where(x => x.IsRequired))
            {
                if (!change.Cells.TryGetValue(required.Id, out var cell) || cell.IsNull != false)
                {
                    return Error.Validation("CellValueInvalid", $"Колонка '{required.AttributeName}' обязательна.");
                }
            }
        }

        return null;
    }

    private static bool IsTransportValueValid(string typeName, string? value)
    {
        var normalized = typeName.ToLowerInvariant();
        if (normalized.Contains("int"))
        {
            return Int64.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
        }

        if (normalized.Contains("decimal") || normalized.Contains("numeric") || normalized.Contains("real") || normalized.Contains("double"))
        {
            return Decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _);
        }

        if (normalized.Contains("bool"))
        {
            return Boolean.TryParse(value, out _) || value is "0" or "1";
        }

        if (normalized.Contains("date") || normalized.Contains("time"))
        {
            return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        }

        return value is not null;
    }

    private static void ApplyCells(
        DataRecord record,
        IReadOnlyDictionary<Guid, TableCellRequest> cells,
        IReadOnlyDictionary<Guid, MetaAttribute> columns,
        AppDbContext db)
    {
        var existing = record.CellValues.ToDictionary(x => x.MetaAttributeId);
        foreach (var (columnId, value) in cells)
        {
            var text = value.IsNull == true ? null : value.Value;
            if (existing.TryGetValue(columnId, out var cell))
            {
                cell.Update(text);
            }
            else
            {
                db.CellValues.Add(CellValue.Create(record.Id, columns[columnId].Id, text));
            }
        }
    }

    private async Task<BatchTableRowsResponse> BuildResponse(
        string schemaVersion,
        IReadOnlyCollection<Guid> changedIds,
        IReadOnlyDictionary<string, Guid> createdIds,
        IReadOnlyDictionary<Guid, MetaAttribute> columns,
        CancellationToken ct)
    {
        var rows = await db.DataRecords.AsNoTracking()
            .Where(x => changedIds.Contains(x.Id))
            .Include(x => x.CellValues)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .ToListAsync(ct);
        return new BatchTableRowsResponse
        {
            SchemaVersion = schemaVersion,
            CreatedIds = createdIds,
            Rows = rows.Select(row => new TableRowResponse
            {
                Id = row.Id,
                Version = row.UpdatedAt.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture),
                SortOrder = row.SortOrder ?? 0,
                Cells = columns.Keys.ToDictionary(columnId => columnId, columnId =>
                {
                    var cell = row.CellValues.SingleOrDefault(x => x.MetaAttributeId == columnId);
                    return new TableCellResponse { Value = cell?.TextValue, IsNull = cell?.TextValue is null };
                })
            }).ToList()
        };
    }
}
