using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.SqlQuery;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.SqlQueries;

public sealed class GetSqlQueryByIdEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Training.SqlQueries.ById, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("GetSqlQueryById")
            .WithTags("Training")
            .WithSummary("Получить SQL-запрос по Id")
            .WithDescription(
                "Возвращает 200 OK с данными запроса. " +
                "404 — запрос с указанным id не найден.")
            .Produces<SqlQueryResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<GetSqlQueryByIdQuery, Result<SqlQueryResponse>>(
            new GetSqlQueryByIdQuery(id), ct);
        return result.ToOk();
    }
}
