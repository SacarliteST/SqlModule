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
                "Публикует подготовленное Draft-задание только если оно готово к запуску студентом: " +
                "опубликована оценка решения, эталон задан и проверен (результат актуален для схемы и данных), " +
                "учебная база существует, у задания нет попыток. Готовность проверяется на backend независимо от клиента; " +
                "полный список причин — в teacher-details (publishBlockers). " +
                "При отказе статус задания не меняется. Код и статус ответа определяет первая причина, " +
                "все причины перечислены в errors. " +
                "Возвращает 200 OK с обновлённым заданием.")
            .Produces<SqlTaskResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> Handle(Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<PublishSqlTaskCommand, Result<SqlTaskResponse>>(
            new PublishSqlTaskCommand(id), ct);
        return result.ToOk();
    }
}
