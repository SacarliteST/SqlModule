using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.MetaTable;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.Schema.MetaTables;

public sealed class GetMetaTableByIdEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Schema.MetaTables.ById, Handle)
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
