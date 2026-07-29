using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.MetaTable;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.MetaTables;

public sealed class UpdateMetaTableEndpoint : IDevEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiRoutes.Schema.MetaTables.ById, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("UpdateMetaTable")
            .WithTags("Schema", "DevTools")
            .WithSummary("Обновить мета-таблицу")
            .WithDescription(
                "dev-only: правка структуры схемы в обход CreateSchema. " +
                "Обновляет название и описание мета-таблицы. " +
                "Возвращает 204 No Content. " +
                "400 — не прошла валидация. " +
                "404 — мета-таблица с указанным id не найдена.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddEndpointFilter<ValidationFilter<UpdateMetaTableRequest>>();
    }

    private static async Task<IResult> Handle(
        Guid id, UpdateMetaTableRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<UpdateMetaTableCommand, Result>(
            MetaTableMappings.ToCommand(id, request), ct);
        return result.ToNoContent();
    }
}
