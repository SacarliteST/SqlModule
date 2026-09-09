using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Data.Core;
using SQLModule.Domain.DbmsCatalog;
using SQLModule.Domain.Schema;
using SQLModule.Sandbox;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.SchemaBuilder.CreateTargetDbFromDdl;

internal sealed record CreateTargetDbFromDdlCommand(
    string IdempotencyKey,
    CreateTargetDbFromDdlRequest Request) : IRequest<Result<CreateTargetDbFromDdlResponse>>;

internal sealed class CreateTargetDbFromDdlHandler(
    IDdlSchemaService service,
    ISchemaSnapshotReader snapshotReader,
    AppDbContext db)
    : IRequestHandler<CreateTargetDbFromDdlCommand, Result<CreateTargetDbFromDdlResponse>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<Result<CreateTargetDbFromDdlResponse>> Handle(
        CreateTargetDbFromDdlCommand command, CancellationToken ct)
    {
        var request = command.Request;
        var scope = $"target-db-from-ddl:{request.DbmsId}";
        var payload = JsonSerializer.Serialize(request, JsonOptions);
        var payloadHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
        var receipt = await db.MutationReceipts.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Scope == scope && x.IdempotencyKey == command.IdempotencyKey, ct);
        if (receipt is not null)
        {
            return receipt.PayloadHash == payloadHash
                ? JsonSerializer.Deserialize<CreateTargetDbFromDdlResponse>(receipt.ResponseJson, JsonOptions)!
                : Result<CreateTargetDbFromDdlResponse>.Fail(Error.Conflict(
                    "IdempotencyKeyPayloadMismatch", "Idempotency-Key уже использован с другим запросом."));
        }

        if (await db.TargetDbs.AnyAsync(
                x => x.DbmsId == request.DbmsId!.Value && x.DbName == request.DbName, ct))
        {
            return Result<CreateTargetDbFromDdlResponse>.Fail(SchemaErrors.AlreadyExists(request.DbName!));
        }

        var prepared = await service.ValidateAndInspectAsync(request.DbmsId!.Value, request.DdlScript!, ct);
        if (!prepared.IsSuccess)
        {
            return Result<CreateTargetDbFromDdlResponse>.Fail(prepared.Error!);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        if (await db.TargetDbs.AnyAsync(
                x => x.DbmsId == request.DbmsId.Value && x.DbName == request.DbName, ct))
        {
            return Result<CreateTargetDbFromDdlResponse>.Fail(SchemaErrors.AlreadyExists(request.DbName!));
        }

        var targetDb = TargetDb.Create(
            request.DbmsId.Value, request.DbName!, request.Description, request.IsReadOnly!.Value);
        db.TargetDbs.Add(targetDb);
        var columns = new Dictionary<(string Table, string Column), Guid>();

        short tableOrder = 0;
        foreach (var inspectedTable in prepared.Value!.Schema.Tables)
        {
            var table = MetaTable.Create(
                targetDb.Id, inspectedTable.Name, null, sortOrder: tableOrder++);
            db.MetaTables.Add(table);
            foreach (var inspectedColumn in inspectedTable.Columns)
            {
                var physicalType = prepared.Value.PhysicalTypes[inspectedColumn.StoreType];
                var column = MetaAttribute.Create(
                    table.Id, physicalType.Id, inspectedColumn.Name, inspectedColumn.IsPrimaryKey,
                    !inspectedColumn.IsNullable, checked((short)inspectedColumn.SortOrder));
                db.MetaAttributes.Add(column);
                columns[(inspectedTable.Name.ToLowerInvariant(), inspectedColumn.Name.ToLowerInvariant())] = column.Id;
                AddParameters(column.Id, physicalType, inspectedColumn, db);
            }
        }

        foreach (var relationship in prepared.Value.Schema.Relationships)
        {
            db.MetaRelationships.Add(MetaRelationship.Create(
                relationship.Name,
                columns[(relationship.SourceTable.ToLowerInvariant(), relationship.SourceColumn.ToLowerInvariant())],
                columns[(relationship.TargetTable.ToLowerInvariant(), relationship.TargetColumn.ToLowerInvariant())],
                relationship.DeleteRule,
                relationship.UpdateRule));
        }

        targetDb.IncrementSchemaVersion();
        await db.SaveChangesAsync(ct);
        var snapshot = await snapshotReader.ReadAsync(targetDb.Id, ct);
        var response = new CreateTargetDbFromDdlResponse(targetDb.Id, snapshot!);
        db.MutationReceipts.Add(MutationReceipt.Create(
            scope, command.IdempotencyKey, payloadHash, JsonSerializer.Serialize(response, JsonOptions)));
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return response;
    }

    private static void AddParameters(
        Guid columnId, PhysicalType physicalType, InspectedColumn column, AppDbContext db)
    {
        foreach (var definition in physicalType.ParameterDefinitions)
        {
            var key = definition.ParameterKey.ToLowerInvariant();
            var value = key.Contains("length") || key.Contains("size")
                ? column.Length
                : key.Contains("precision") ? column.Precision
                : key.Contains("scale") ? column.Scale
                : null;
            if (value.HasValue)
            {
                db.AttributeParameterValues.Add(AttributeParameterValue.Create(
                    columnId, definition.Id, value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            }
        }
    }
}
