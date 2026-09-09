using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Data.Core;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.SchemaBuilder.ValidateTargetDbSchema;

internal sealed record ValidateTargetDbSchemaCommand(Guid TargetDbId, SchemaUpsertRequest Request)
    : IRequest<Result<SchemaValidationResponse>>;

internal sealed class ValidateTargetDbSchemaHandler(
    ISchemaPreparer preparer,
    ISchemaDiffPlanner planner,
    AppDbContext db)
    : IRequestHandler<ValidateTargetDbSchemaCommand, Result<SchemaValidationResponse>>
{
    public async Task<Result<SchemaValidationResponse>> Handle(
        ValidateTargetDbSchemaCommand command,
        CancellationToken ct)
    {
        var targetDb = await db.TargetDbs.AsNoTracking()
            .Where(x => x.Id == command.TargetDbId)
            .Select(x => new { x.DbmsId, x.DbName, x.IsReadOnly, x.SchemaVersion })
            .SingleOrDefaultAsync(ct);
        if (targetDb is null)
        {
            return Result<SchemaValidationResponse>.Fail(SchemaAggregateErrors.NotFound(command.TargetDbId));
        }

        if (targetDb.IsReadOnly)
        {
            return Result<SchemaValidationResponse>.Fail(SchemaAggregateErrors.EditingForbidden(command.TargetDbId));
        }

        var request = SchemaUpsertMapper.ToCreateRequest(targetDb.DbmsId, targetDb.DbName, command.Request);
        var validation = await preparer.PrepareAndValidateAsync(request, ct);
        if (!validation.IsSuccess)
        {
            return Result<SchemaValidationResponse>.Fail(validation.Error!);
        }

        var plan = await planner.BuildAsync(command.TargetDbId, command.Request, ct);
        return new SchemaValidationResponse
        {
            IsValid = plan.IsSafe,
            RequiresConfirmation = false,
            NormalizedVersion = targetDb.SchemaVersion.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Warnings = plan.DestructiveChanges.Select(x => x.Description).ToList(),
            Changes = plan.Changes,
            DestructiveChanges = plan.DestructiveChanges,
            ConfirmationToken = null,
            DdlPreview = null
        };
    }
}
