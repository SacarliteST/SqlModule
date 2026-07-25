using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Attempt;
using SQLModule.Domain.Common;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.Attempts.SubmitAttempt;

public sealed class SubmitAttemptEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Training.Attempts.Collection, Handle)
            .RequireAuthorization(Policies.Student)
            .WithName("SubmitAttempt")
            .WithTags("Training")
            .WithSummary("Отправить попытку выполнения задания")
            .WithDescription(
                "Запускает SQL студента в песочнице, сравнивает с эталоном и сохраняет результат. " +
                "UserId берётся из текущей сессии. " +
                "Возвращает 201 Created с результатом проверки. " +
                "400 — не прошла валидация. " +
                "404 — задание не найдено. " +
                "409 — эталонный результат задания ещё не готов.")
            .Produces<SubmitAttemptResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .AddEndpointFilter<ValidationFilter<SubmitAttemptRequest>>();
    }

    private static async Task<IResult> Handle(
        SubmitAttemptRequest request, ISender sender, ICurrentUser currentUser, CancellationToken ct)
    {
        var userId = currentUser.UserId!.Value;
        var result = await sender.Send<SubmitAttemptCommand, Result<SubmitAttemptResponse>>(
            new SubmitAttemptCommand(
                userId,
                currentUser.DisplayName ?? userId.ToString(),
                request.TaskId,
                request.SubmittedSql), ct);
        return result.ToCreated(r => ApiRoutes.Training.Attempts.ForId(r.AttemptId));
    }
}
