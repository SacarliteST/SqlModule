using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Attempt;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.Training.Attempts;

public sealed class UpdateAttemptEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiRoutes.Training.Attempts.ById, Handle)
            .WithName("UpdateAttempt")
            .WithTags("Training")
            .WithSummary("Обновить результат попытки")
            .WithDescription(
                "Обновляет признак успешности и время завершения попытки. " +
                "Возвращает 204 No Content. " +
                "400 — не прошла валидация. " +
                "404 — попытка с указанным id не найдена. " +
                "409 — новое время завершения раньше времени начала попытки.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .AddEndpointFilter<ValidationFilter<UpdateAttemptRequest>>();
    }

    private static async Task<IResult> Handle(
        Guid id, UpdateAttemptRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<UpdateAttemptCommand, Result>(
            AttemptMappings.ToCommand(id, request), ct);
        return result.ToNoContent();
    }
}
