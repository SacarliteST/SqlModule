using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.SqlQuery;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.Training.SqlQueries;

public sealed class UpdateSqlQueryEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiRoutes.Training.SqlQueries.ById, Handle)
            .WithName("UpdateSqlQuery")
            .WithTags("Training")
            .WithSummary("Обновить SQL-запрос")
            .WithDescription(
                "Обновляет текст запроса и настройки сравнения. " +
                "Возвращает 204 No Content. " +
                "400 — не прошла валидация. " +
                "404 — запрос с указанным id не найден.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddEndpointFilter<ValidationFilter<UpdateSqlQueryRequest>>();
    }

    private static async Task<IResult> Handle(
        Guid id, UpdateSqlQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<UpdateSqlQueryCommand, Result>(
            SqlQueryMappings.ToCommand(id, request), ct);
        return result.ToNoContent();
    }
}
