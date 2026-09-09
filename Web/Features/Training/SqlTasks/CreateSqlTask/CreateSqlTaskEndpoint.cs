using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.SqlTask;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.SqlTasks;

public sealed class CreateSqlTaskEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Training.SqlTasks.Collection, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("CreateSqlTask")
            .WithTags("Training")
            .WithSummary("Создать SQL-задание")
            .WithDescription(
                "Проверяет и создаёт эталонное решение вместе с новым заданием в статусе Draft. " +
                "Эталон, превышающий серверный лимит сравнения, отклоняется с кодом " +
                "ReferenceResultExceedsComparisonLimit. " +
                "Для публикации используйте отдельную операцию. " +
                "Возвращает 201 Created с телом ответа. " +
                "422 — не прошла бизнес-валидация. " +
                "409 — тема или учебная база не найдены.")
            .Produces<SqlTaskResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .AddEndpointFilter<ValidationFilter<CreateSqlTaskRequest>>();
    }

    private static async Task<IResult> Handle(
        CreateSqlTaskRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<CreateSqlTaskCommand, Result<SqlTaskResponse>>(
            SqlTaskMappings.ToCommand(request), ct);
        return result.ToCreated(r => ApiRoutes.Training.SqlTasks.ForId(r.Id));
    }
}
