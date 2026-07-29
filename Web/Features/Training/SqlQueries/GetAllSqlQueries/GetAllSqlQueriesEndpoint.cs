using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.SqlQuery;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.SqlQueries;

public sealed class GetAllSqlQueriesEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Training.SqlQueries.Collection, Handle)
            .WithName("GetAllSqlQueries")
            .WithTags("Training")
            .WithSummary("Список SQL-запросов с пагинацией")
            .WithDescription(
                "Возвращает 200 OK со страницей запросов, отсортированных по дате создания. " +
                "offset — количество пропускаемых записей (>= 0, по умолчанию 0). " +
                "limit — размер страницы (1–100, по умолчанию 20). " +
                "400 — невалидные параметры пагинации.")
            .Produces<PageResponse<SqlQueryResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .AddEndpointFilter<ValidationFilter<GetAllSqlQueriesRequest>>();
    }

    private static async Task<IResult> Handle(
        [AsParameters] GetAllSqlQueriesRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<GetAllSqlQueriesQuery, Result<PageResponse<SqlQueryResponse>>>(
            new GetAllSqlQueriesQuery(request.Offset, request.Limit), ct);
        return result.ToOk();
    }
}
