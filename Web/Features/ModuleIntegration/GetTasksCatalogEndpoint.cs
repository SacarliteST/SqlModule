using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SQLModule.Contracts;
using SQLModule.Contracts.ModuleIntegration;
using SQLModule.Data.Core;
using SQLModule.Domain.Training;
using SQLModule.Web.Common;

namespace SQLModule.Web.Features.ModuleIntegration;

internal sealed class GetTasksCatalogEndpoint : IModuleIntegrationEndpoint
{
    private const int DescriptionMaxLength = 200;

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
        AppDbContext db,
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

        var items = await db.SqlTasks.AsNoTracking()
            .Where(task => task.PublicationStatus == PublicationStatus.Published)
            .OrderBy(task => task.TaskName)
            .ThenBy(task => task.Id)
            .Select(task => new ModuleTaskCatalogItemResponse(
                task.Id.ToString(),
                task.TaskName,
                task.TaskText.Length <= DescriptionMaxLength
                    ? task.TaskText
                    : task.TaskText.Substring(0, DescriptionMaxLength)))
            .ToListAsync(ct);

        return TypedResults.Ok<IReadOnlyList<ModuleTaskCatalogItemResponse>>(items);
    }
}
