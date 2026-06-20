using SQLModule.Contracts;
using SQLModule.Contracts.Schema.MetaTable;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Schema.MetaTables;

public sealed class UpdateMetaTableEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiRoutes.Schema.MetaTables.ById, Handle)
            .WithName("UpdateMetaTable")
            .WithTags("Schema")
            .WithSummary("Обновить мета-таблицу")
            .WithDescription(
                "Обновляет название и описание мета-таблицы. " +
                "Возвращает 204 No Content. " +
                "400 — не прошла валидация. " +
                "404 — мета-таблица с указанным id не найдена.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
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
