using SQLModule.Application.ModuleIntegration;
﻿using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SQLModule.Application.Cqrs;
using SQLModule.Common.Results;
using Microsoft.Extensions.Options;
using SQLModule.Contracts;
using SQLModule.Contracts.ModuleIntegration;
using SQLModule.Web.Common;

namespace SQLModule.Web.Features.ModuleIntegration;

internal sealed class GetTasksCatalogEndpoint : IModuleIntegrationEndpoint
{

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.ModuleIntegration.TasksCatalog, Handle)
            .AllowAnonymous()
            .WithName("GetModuleTasksCatalog")
            .WithTags("Module Integration")
            .WithSummary("Получить каталог опубликованных SQL-заданий")
            .WithDescription(
                "Сервер-сервер каталог для Education. Требует X-Service-Key и возвращает только Published-задания.")
            .Produces<IReadOnlyList<ModuleTaskCatalogItemResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .AddEndpointFilter<ModuleIntegrationServiceKeyFilter>();
    }

    private static async Task<IResult> Handle(
        [FromHeader(Name = "X-Service-Key"), Required] string? serviceKey,
        IOptions<ModuleIntegrationOptions> options,
        ISender sender,
        CancellationToken ct)
    {
        if (!ServiceKeyValidator.IsValid(serviceKey, options.Value.ServiceKey))
        {
            return ApiProblemFactory.ToResult(
                StatusCodes.Status401Unauthorized,
                "Доступ запрещён",
                "Не удалось авторизовать серверный запрос.",
                "InvalidServiceKey");
        }

        var result = await sender.Send<GetModuleTasksCatalogQuery, Result<IReadOnlyList<ModuleTaskCatalogItemResponse>>>(
            new GetModuleTasksCatalogQuery(), ct);
        return result.ToOk();
    }
}
