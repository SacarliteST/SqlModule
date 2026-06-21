using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Attempt;
using SQLModule.Domain.Common;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.Training.Attempts;

public sealed class CreateAttemptEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Training.Attempts.Collection, Handle)
            .WithName("CreateAttempt")
            .WithTags("Training")
            .WithSummary("Зафиксировать попытку выполнения задания")
            .WithDescription(
                "Создаёт запись о попытке выполнения задания. " +
                "UserId берётся из текущей сессии (не из тела запроса). " +
                "Возвращает 201 Created с телом ответа. " +
                "400 — не прошла валидация. " +
                "409 — задание или SQL-запрос не найдены.")
            .Produces<AttemptResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .AddEndpointFilter<ValidationFilter<CreateAttemptRequest>>();
    }

    private static async Task<IResult> Handle(
        CreateAttemptRequest request, ISender sender, ICurrentUser currentUser, CancellationToken ct)
    {
        // TODO: заменить после Identity — требовать реального пользователя (401)
        var result = await sender.Send<CreateAttemptCommand, Result<AttemptResponse>>(
            AttemptMappings.ToCommand(request, currentUser.UserId ?? SystemUser.Id), ct);
        return result.ToCreated(r => ApiRoutes.Training.Attempts.ForId(r.Id));
    }
}
