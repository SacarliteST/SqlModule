using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.MetaTable;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.MetaTables;

public sealed class GetMetaTableByIdEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Schema.MetaTables.ById, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("GetMetaTableById")
            .WithTags("Schema")
            .WithSummary("Получить мета-таблицу по Id")
            .WithDescription(
                "Возвращает 200 OK с данными мета-таблицы. " +
                "404 — мета-таблица с указанным id не найдена.")
            .Produces<MetaTableResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<GetMetaTableByIdQuery, Result<MetaTableResponse>>(
            new GetMetaTableByIdQuery(id), ct);
        return result.ToOk();
    }
}
