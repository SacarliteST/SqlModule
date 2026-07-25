using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.SqlTask;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.SqlTasks;

public sealed class PublishSqlTaskEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Training.SqlTasks.Publish, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("PublishSqlTask")
            .WithTags("Training")
            .WithSummary("Опубликовать SQL-задание")
            .WithDescription(
                "Публикует подготовленное Draft-задание. Эталонный запрос должен иметь проверенный результат, " +
                "учебная база должна существовать, а у задания не должно быть попыток. " +
                "Возвращает 200 OK с обновлённым заданием.")
            .Produces<SqlTaskResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> Handle(Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<PublishSqlTaskCommand, Result<SqlTaskResponse>>(
            new PublishSqlTaskCommand(id), ct);
        return result.ToOk();
    }
}
