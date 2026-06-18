using SQLModule.Contracts;
using SQLModule.Contracts.Schema.TargetDb;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Schema.TargetDbs;

public sealed class CreateTargetDbEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Schema.TargetDbs.Collection, Handle)
            .WithName("CreateTargetDb")
            .WithTags("Schema")
            .Produces<TargetDbResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .AddEndpointFilter<ValidationFilter<CreateTargetDbRequest>>();
    }

    private static async Task<IResult> Handle(
        CreateTargetDbRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<CreateTargetDbCommand, Result<TargetDbResponse>>(
            TargetDbMappings.ToCommand(request), ct);
        return result.ToCreated(r => ApiRoutes.Schema.TargetDbs.ForId(r.Id));
    }
}
