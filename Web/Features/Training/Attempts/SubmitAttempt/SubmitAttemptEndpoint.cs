using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Attempt;
using SQLModule.Domain.Common;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;
using SQLModule.Web.Features.ModuleIntegration;

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
                "Требует UUID в заголовке Idempotency-Key. Ключ уникален для текущего студента и действует 24 часа. " +
                "Повтор с тем же телом возвращает исходную попытку без повторного запуска sandbox; другое тело возвращает 409. " +
                "В platform-профиле session_id берётся из JWT и сверяется с владельцем, заданием и сроком сессии. " +
                "Возвращает 201 Created с результатом проверки. " +
                "400 — Idempotency-Key отсутствует или некорректен. " +
                "404 — задание не найдено. " +
                "409 — эталонный результат задания ещё не готов.")
            .Produces<SubmitAttemptResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .AddEndpointFilter<ValidationFilter<SubmitAttemptRequest>>();
    }

    private static async Task<IResult> Handle(
        SubmitAttemptRequest request,
        [FromHeader(Name = "Idempotency-Key"), Required, StringLength(128)] string? idempotencyKey,
        ISender sender,
        ICurrentUser currentUser,
        IOptions<ModuleIntegrationOptions> integrationOptions,
        CancellationToken ct)
    {
        if (String.IsNullOrWhiteSpace(idempotencyKey) ||
            idempotencyKey.Length > 128 ||
            idempotencyKey.Any(Char.IsControl) ||
            !Guid.TryParseExact(idempotencyKey, "D", out var parsedKey))
        {
            return ApiProblemFactory.ToResult(
                StatusCodes.Status400BadRequest,
                "Некорректный Idempotency-Key",
                "Заголовок Idempotency-Key должен содержать UUID в формате xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx.",
                "InvalidIdempotencyKey",
                new Dictionary<string, string[]>
                {
                    ["Idempotency-Key"] = ["Укажите корректный UUID длиной не более 128 символов."]
                });
        }

        var userId = currentUser.UserId!.Value;
        var result = await sender.Send<SubmitAttemptCommand, Result<SubmitAttemptResponse>>(
            new SubmitAttemptCommand(
                userId,
                currentUser.DisplayName ?? userId.ToString(),
                currentUser.Email,
                request.TaskId,
                request.SubmittedSql,
                parsedKey.ToString("D"),
                integrationOptions.Value.Enabled,
                integrationOptions.Value.Enabled ? currentUser.ModuleSessionId : null), ct);
        return result.ToCreated(r => ApiRoutes.Training.Attempts.ForId(r.AttemptId));
    }
}
