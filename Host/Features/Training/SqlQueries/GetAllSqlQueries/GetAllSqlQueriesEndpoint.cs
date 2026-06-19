using SQLModule.Contracts;
using SQLModule.Contracts.Training.SqlQuery;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Training.SqlQueries;

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
            .ProducesValidationProblem()
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
