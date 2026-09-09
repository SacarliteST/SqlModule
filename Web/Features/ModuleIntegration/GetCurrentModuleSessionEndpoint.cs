using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Contracts.ModuleIntegration;
using SQLModule.Data.Core;
using SQLModule.Domain.Common;
using SQLModule.Web.Common;

namespace SQLModule.Web.Features.ModuleIntegration;

internal sealed class GetCurrentModuleSessionEndpoint : IModuleIntegrationEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.ModuleIntegration.CurrentSession, Handle)
            .RequireAuthorization(Policies.Student)
            .WithName("GetCurrentModuleSession")
            .WithTags("Module Integration")
            .WithSummary("Получить текущую платформенную сессию")
            .WithDescription(
                "Берёт session_id и пользователя только из валидированного JWT. " +
                "Отсутствующая, неизвестная или чужая сессия скрывается единым ответом 404.")
            .Produces<CurrentModuleSessionResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        ICurrentUser currentUser,
        AppDbContext db,
        CancellationToken ct)
    {
        using var activity = ModuleIntegrationTelemetry.StartActivity("module-session.current");
        if (currentUser is not { UserId: { } userId, ModuleSessionId: { } sessionId })
        {
            ModuleIntegrationTelemetry.RecordCurrent(ModuleIntegrationTelemetryOutcomes.NotFound);
            return NotFound();
        }

        var session = await db.ModuleSessions.AsNoTracking()
            .Where(value => value.Id == sessionId && value.UserId == userId)
            .Select(value => new
            {
                value.TaskRef,
                value.ReturnUrl,
                value.ExpiresAt
            })
            .SingleOrDefaultAsync(ct);

        if (session is null || !Guid.TryParse(session.TaskRef, out var taskId))
        {
            ModuleIntegrationTelemetry.RecordCurrent(ModuleIntegrationTelemetryOutcomes.NotFound);
            return NotFound();
        }

        ModuleIntegrationTelemetry.RecordCurrent(ModuleIntegrationTelemetryOutcomes.Found);
        return TypedResults.Ok(new CurrentModuleSessionResponse(
            taskId,
            session.ReturnUrl,
            session.ExpiresAt));
    }

    private static IResult NotFound() => ApiProblemFactory.ToResult(
        StatusCodes.Status404NotFound,
        "Ресурс не найден",
        "Платформенная сессия не найдена или недоступна.",
        "ModuleSession.NotFound");
}
