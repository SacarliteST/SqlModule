using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.ModuleIntegration;
using SQLModule.Data.Core;
using SQLModule.Domain.ModuleIntegration;
using SQLModule.Web.Common;
using SQLModule.Web.Features.Training.Progress;

namespace SQLModule.Web.Features.ModuleIntegration;

internal sealed class UpsertModuleSessionEndpoint : IModuleIntegrationEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.ModuleIntegration.Sessions, Handle)
            .AllowAnonymous()
            .WithName("UpsertModuleSession")
            .WithTags("Module Integration")
            .WithSummary("Создать или восстановить платформенную сессию")
            .WithDescription(
                "Сервер-сервер push от Education. Повторный push синхронизирует Active-сессию; " +
                "Completed-сессия отвечает 409 и не изменяется.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .AddEndpointFilter<ModuleIntegrationServiceKeyFilter>()
            .AddEndpointFilter<ValidationFilter<UpsertModuleSessionRequest>>();
    }

    private static async Task<IResult> Handle(
        UpsertModuleSessionRequest request,
        [FromHeader(Name = "X-Service-Key"), Required] string? serviceKey,
        IOptions<ModuleIntegrationOptions> options,
        IPlatformProgressService progressService,
        AppDbContext db,
        CancellationToken ct)
    {
        using var activity = ModuleIntegrationTelemetry.StartActivity("module-session.push");
        if (!ServiceKeyValidator.IsValid(serviceKey, options.Value.ServiceKey))
        {
            ModuleIntegrationTelemetry.RecordPush(ModuleIntegrationTelemetryOutcomes.Unauthorized);
            return ApiProblemFactory.ToResult(
                StatusCodes.Status401Unauthorized,
                "Доступ запрещён",
                "Не удалось авторизовать серверный запрос.",
                "InvalidServiceKey");
        }

        var sessionId = request.SessionId!.Value;
        var session = await db.ModuleSessions.SingleOrDefaultAsync(value => value.Id == sessionId, ct);

        if (session is null)
        {
            var created = ModuleSession.Create(
                sessionId,
                request.SessionKey!,
                request.UserId!.Value,
                request.TaskRef!,
                request.ReturnUrl!,
                request.ExpiresAt);
            db.ModuleSessions.Add(created);

            var progressResult = await progressService.EnsureCreatedAsync(created, ct);
            if (!progressResult.IsSuccess && !IsValidationVersionPending(progressResult.Error))
            {
                return progressResult.ToOk();
            }

            try
            {
                await db.SaveChangesAsync(ct);
                ModuleIntegrationTelemetry.RecordPush(ModuleIntegrationTelemetryOutcomes.Created);
                return TypedResults.Ok();
            }
            catch (DbUpdateException)
            {
                // Параллельный повтор того же push мог вставить строку после SELECT.
                // Если причина другая, повторное чтение не найдёт сессию и исходная ошибка уйдёт выше.
                db.ChangeTracker.Clear();
                session = await db.ModuleSessions.SingleOrDefaultAsync(value => value.Id == sessionId, ct);
                if (session is null)
                {
                    throw;
                }
            }
        }

        if (session.Status != ModuleSessionStatus.Active)
        {
            ModuleIntegrationTelemetry.RecordPush(ModuleIntegrationTelemetryOutcomes.Conflict);
            return ApiProblemFactory.ToResult(
                StatusCodes.Status409Conflict,
                "Конфликт состояния",
                "Неактивную платформенную сессию нельзя обновить.",
                "ModuleSession.Completed");
        }

        session.Synchronize(
            request.SessionKey!,
            request.UserId!.Value,
            request.TaskRef!,
            request.ReturnUrl!,
            request.ExpiresAt);
        var existingProgressResult = await progressService.EnsureCreatedAsync(session, ct);
        if (!existingProgressResult.IsSuccess && !IsValidationVersionPending(existingProgressResult.Error))
        {
            return existingProgressResult.ToOk();
        }
        await db.SaveChangesAsync(ct);
        ModuleIntegrationTelemetry.RecordPush(ModuleIntegrationTelemetryOutcomes.Updated);
        return TypedResults.Ok();
    }

    private static bool IsValidationVersionPending(SQLModule.Common.Results.Error? error) =>
        error?.Code == "ModuleSession.TaskValidationUnavailable";
}
