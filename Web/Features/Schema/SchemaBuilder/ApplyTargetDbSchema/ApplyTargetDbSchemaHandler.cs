using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Data.Core;
using SQLModule.Domain.Schema;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.SchemaBuilder.ApplyTargetDbSchema;

internal sealed record ApplyTargetDbSchemaCommand(
    Guid TargetDbId,
    string? IfMatch,
    string IdempotencyKey,
    SchemaUpsertRequest Request) : IRequest<Result<TargetDbSchemaResponse>>;

internal sealed class ApplyTargetDbSchemaHandler(
    ISchemaPreparer preparer,
    ISchemaDiffPlanner planner,
    ISchemaDiffApplier applier,
    ISchemaSnapshotReader snapshotReader,
    AppDbContext db)
    : IRequestHandler<ApplyTargetDbSchemaCommand, Result<TargetDbSchemaResponse>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<Result<TargetDbSchemaResponse>> Handle(
        ApplyTargetDbSchemaCommand command,
        CancellationToken ct)
    {
        var scope = $"schema:{command.TargetDbId}";
        var payloadJson = JsonSerializer.Serialize(command.Request, JsonOptions);
        var payloadHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payloadJson)));
        var receipt = await db.MutationReceipts.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Scope == scope && x.IdempotencyKey == command.IdempotencyKey, ct);
        if (receipt is not null)
        {
            return receipt.PayloadHash == payloadHash
                ? JsonSerializer.Deserialize<TargetDbSchemaResponse>(receipt.ResponseJson, JsonOptions)!
                : Result<TargetDbSchemaResponse>.Fail(Error.Conflict(
                    "IdempotencyKeyPayloadMismatch", "Idempotency-Key уже использован с другим запросом."));
        }

        var targetInfo = await db.TargetDbs.AsNoTracking()
            .Where(x => x.Id == command.TargetDbId)
            .Select(x => new { x.DbmsId, x.DbName })
            .SingleOrDefaultAsync(ct);
        if (targetInfo is null)
        {
            return Result<TargetDbSchemaResponse>.Fail(SchemaAggregateErrors.NotFound(command.TargetDbId));
        }

        var prepared = await preparer.PrepareAndValidateAsync(
            SchemaUpsertMapper.ToCreateRequest(targetInfo.DbmsId, targetInfo.DbName, command.Request), ct);
        if (!prepared.IsSuccess)
        {
            return Result<TargetDbSchemaResponse>.Fail(prepared.Error!);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var targetDb = await db.TargetDbs.SingleAsync(x => x.Id == command.TargetDbId, ct);
        if (targetDb.IsReadOnly)
        {
            return Result<TargetDbSchemaResponse>.Fail(SchemaAggregateErrors.EditingForbidden(command.TargetDbId));
        }

        var actualVersion = targetDb.SchemaVersion.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var headerVersion = command.IfMatch?.Trim().Trim('"');
        if (command.Request.Version != actualVersion ||
            (!String.IsNullOrWhiteSpace(headerVersion) && headerVersion != actualVersion))
        {
            return Result<TargetDbSchemaResponse>.Fail(
                SchemaAggregateErrors.VersionConflict(command.Request.Version ?? "<missing>", actualVersion));
        }

        var tableIds = await db.MetaTables.AsNoTracking()
            .Where(x => x.TargetDbId == command.TargetDbId)
            .Select(x => x.Id)
            .ToListAsync(ct);
        var columnIds = await db.MetaAttributes.AsNoTracking()
            .Where(x => tableIds.Contains(x.MetaTableId))
            .Select(x => x.Id)
            .ToListAsync(ct);
        if (HasUnknownExistingIds(command.Request, tableIds, columnIds,
                await db.MetaRelationships.AsNoTracking()
                    .Where(x => columnIds.Contains(x.SourceAttributeId))
                    .Select(x => x.Id).ToListAsync(ct)))
        {
            return Result<TargetDbSchemaResponse>.Fail(
                Error.Validation("SchemaValidationFailed", "Схема содержит неизвестные постоянные идентификаторы."));
        }

        var plan = await planner.BuildAsync(command.TargetDbId, command.Request, ct);
        if (!plan.IsSafe)
        {
            var blocked = plan.DestructiveChanges[0];
            return Result<TargetDbSchemaResponse>.Fail(Error.Conflict(
                blocked.Code ?? "SchemaChangeBlockedByData", blocked.Description,
                blocked.Path, blocked.AffectedRows));
        }

        await applier.ApplyAsync(command.TargetDbId, command.Request, ct);

        targetDb.IncrementSchemaVersion();
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<TargetDbSchemaResponse>.Fail(
                SchemaAggregateErrors.VersionConflict(command.Request.Version!, actualVersion));
        }

        var snapshot = await snapshotReader.ReadAsync(command.TargetDbId, ct);
        db.MutationReceipts.Add(MutationReceipt.Create(
            scope, command.IdempotencyKey, payloadHash, JsonSerializer.Serialize(snapshot, JsonOptions)));
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return snapshot!;
    }

    private static bool HasUnknownExistingIds(
        SchemaUpsertRequest request,
        IReadOnlyCollection<Guid> tableIds,
        IReadOnlyCollection<Guid> columnIds,
        IReadOnlyCollection<Guid> relationshipIds)
    {
        return request.Tables!.Any(x => x.Id.HasValue && !tableIds.Contains(x.Id.Value)) ||
               request.Tables!.SelectMany(x => x.Columns!).Any(x => x.Id.HasValue && !columnIds.Contains(x.Id.Value)) ||
               request.Relationships!.Any(x => x.Id.HasValue && !relationshipIds.Contains(x.Id.Value));
    }
}
