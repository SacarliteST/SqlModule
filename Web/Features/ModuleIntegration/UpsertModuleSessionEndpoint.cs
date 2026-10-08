using SQLModule.Application.ModuleIntegration;
﻿using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SQLModule.Application.Cqrs;
using Microsoft.Extensions.Options;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.ModuleIntegration;
using SQLModule.Web.Common;

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
        ISender sender,
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

        var result = await sender.Send<UpsertModuleSessionCommand, Result>(new UpsertModuleSessionCommand(request), ct);
        if (result.Error?.Code == ModuleSessionErrors.CompletedCode)
        {
            return ApiProblemFactory.ToResult(
                StatusCodes.Status409Conflict,
                "Конфликт состояния",
                "Неактивную платформенную сессию нельзя обновить.",
                ModuleSessionErrors.CompletedCode);
        }

        return result.IsSuccess ? TypedResults.Ok() : result.ToOk();
    }
}
