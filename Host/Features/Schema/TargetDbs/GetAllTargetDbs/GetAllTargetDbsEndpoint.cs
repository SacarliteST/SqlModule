using SQLModule.Contracts;
using SQLModule.Contracts.Schema.TargetDb;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Schema.TargetDbs;

public sealed class GetAllTargetDbsEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Schema.TargetDbs.Collection, Handle)
            .WithName("GetAllTargetDbs")
            .WithTags("Schema")
            .Produces<PageResponse<TargetDbResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<GetAllTargetDbsRequest>>();
    }

    private static async Task<IResult> Handle(
        [AsParameters] GetAllTargetDbsRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<GetAllTargetDbsQuery, Result<PageResponse<TargetDbResponse>>>(
            new GetAllTargetDbsQuery(request.Offset, request.Limit), ct);
        return result.ToOk();
    }
}
