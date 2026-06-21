using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.Schema.AttributeParameterValues;

internal sealed class DeleteAttributeParameterValueEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapDelete(ApiRoutes.Schema.AttributeParameterValues.ById, Handle)
            .WithName("DeleteAttributeParameterValue")
            .WithTags("Schema")
            .WithSummary("Удалить значение параметра атрибута")
            .WithDescription(
                "Удаляет значение параметра физического типа для колонки. " +
                "На сущность никто не ссылается — конфликтов не возникает. " +
                "Возвращает 204 No Content. " +
                "404 — запись с указанным Id не найдена.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<DeleteAttributeParameterValueCommand, Result>(
            new DeleteAttributeParameterValueCommand(id), ct);
        return result.ToNoContent();
    }
}
