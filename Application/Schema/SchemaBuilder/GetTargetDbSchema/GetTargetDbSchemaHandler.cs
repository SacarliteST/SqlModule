using SQLModule.Application.Cqrs;
using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.SchemaBuilder;

namespace SQLModule.Application.Schema.SchemaBuilder.GetTargetDbSchema;

public sealed record GetTargetDbSchemaQuery(Guid TargetDbId)
    : IRequest<Result<TargetDbSchemaResponse>>;

public sealed class GetTargetDbSchemaHandler(ISchemaSnapshotReader reader)
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
