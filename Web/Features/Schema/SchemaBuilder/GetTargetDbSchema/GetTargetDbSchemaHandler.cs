using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.SchemaBuilder.GetTargetDbSchema;

internal sealed record GetTargetDbSchemaQuery(Guid TargetDbId)
    : IRequest<Result<TargetDbSchemaResponse>>;

internal sealed class GetTargetDbSchemaHandler(ISchemaSnapshotReader reader)
    : IRequestHandler<GetTargetDbSchemaQuery, Result<TargetDbSchemaResponse>>
{
    public async Task<Result<TargetDbSchemaResponse>> Handle(
        GetTargetDbSchemaQuery query,
        CancellationToken ct)
    {
        var snapshot = await reader.ReadAsync(query.TargetDbId, ct);
        return snapshot is null
            ? Result<TargetDbSchemaResponse>.Fail(SchemaAggregateErrors.NotFound(query.TargetDbId))
            : snapshot;
    }
}
