using SQLModule.Contracts;
using SQLModule.Contracts.Training.Attempt;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Training.Attempts;

public sealed class GetAttemptByIdEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Training.Attempts.ById, Handle)
            .WithName("GetAttemptById")
            .WithTags("Training")
            .WithSummary("Получить попытку по Id")
            .WithDescription(
                "Возвращает 200 OK с данными попытки. " +
                "404 — попытка с указанным id не найдена.")
            .Produces<AttemptResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<GetAttemptByIdQuery, Result<AttemptResponse>>(
            new GetAttemptByIdQuery(id), ct);
        return result.ToOk();
    }
}
