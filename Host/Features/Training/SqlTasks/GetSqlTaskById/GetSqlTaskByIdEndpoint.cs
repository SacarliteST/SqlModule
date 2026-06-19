using SQLModule.Contracts;
using SQLModule.Contracts.Training.SqlTask;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Training.SqlTasks;

public sealed class GetSqlTaskByIdEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Training.SqlTasks.ById, Handle)
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
