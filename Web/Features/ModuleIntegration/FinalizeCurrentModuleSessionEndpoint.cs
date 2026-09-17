using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Validation;
using SQLModule.Domain.Common;
using SQLModule.Web.Common;
using SQLModule.Web.Features.Training.Progress;

namespace SQLModule.Web.Features.ModuleIntegration;

internal sealed class FinalizeCurrentModuleSessionEndpoint : IModuleIntegrationEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app) =>
        app.MapPost(ApiRoutes.ModuleIntegration.FinalizeCurrentSession, Handle)
            .RequireAuthorization(Policies.Student)
            .WithName("FinalizeCurrentModuleSession")
            .WithTags("Module Integration")
            .WithSummary("Завершить текущее платформенное прохождение")
            .WithDescription("Фиксирует фактический BestScore и атомарно ставит один итог в outbox Education.")
            .Produces<ProgressFinalizationResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);

    private static async Task<IResult> Handle(
        [FromHeader(Name = "Idempotency-Key"), Required] string? idempotencyKey,
        ICurrentUser currentUser,
        IProgressFinalizationService service,
        CancellationToken ct)
    {
        if (!Guid.TryParseExact(idempotencyKey, "D", out var key))
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]> { ["Idempotency-Key"] = ["Укажите UUID в формате D."] },
                statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        if (currentUser is not { UserId: { } userId, ModuleSessionId: { } sessionId })
        {
            return ApiProblemFactory.ToResult(
                StatusCodes.Status404NotFound,
                "Ресурс не найден",
                "Платформенная сессия не найдена или недоступна.",
                "ModuleSession.NotFound");
        }

        return (await service.FinalizePlatformAsync(userId, sessionId, key, ct)).ToOk();
    }
}
