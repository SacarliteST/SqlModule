using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.SqlTask;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.SqlTasks;

public sealed class GetAllSqlTasksEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Training.SqlTasks.Collection, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("GetAllSqlTasks")
            .WithTags("Training")
            .WithSummary("Список SQL-заданий с пагинацией")
            .WithDescription(
                "Возвращает 200 OK со страницей заданий, отсортированных по сложности, затем по названию. " +
                "offset — количество пропускаемых записей (>= 0, по умолчанию 0). " +
                "limit — размер страницы (1–100, по умолчанию 20). " +
                "400 — невалидные параметры пагинации.")
            .Produces<PageResponse<SqlTaskResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .AddEndpointFilter<ValidationFilter<GetAllSqlTasksRequest>>();
    }

    private static async Task<IResult> Handle(
        [AsParameters] GetAllSqlTasksRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<GetAllSqlTasksQuery, Result<PageResponse<SqlTaskResponse>>>(
            new GetAllSqlTasksQuery(
                request.Offset, request.Limit, request.TopicId, request.Name,
                request.TargetDbId, request.DifficultyLevel, request.PublicationStatus), ct);
        return result.ToOk();
    }
}
