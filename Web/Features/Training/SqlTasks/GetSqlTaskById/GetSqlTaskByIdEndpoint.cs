using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.SqlTask;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.SqlTasks;

public sealed class GetSqlTaskByIdEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Training.SqlTasks.ById, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("GetSqlTaskById")
            .WithTags("Training")
            .WithSummary("Получить SQL-задание по Id")
            .WithDescription(
                "Возвращает 200 OK с данными задания. " +
                "404 — задание с указанным id не найдено.")
            .Produces<SqlTaskResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<GetSqlTaskByIdQuery, Result<SqlTaskResponse>>(
            new GetSqlTaskByIdQuery(id), ct);
        return result.ToOk();
    }
}
