using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.SqlTask;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.Training.SqlTasks;

public sealed class CreateSqlTaskEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Training.SqlTasks.Collection, Handle)
            .WithName("CreateSqlTask")
            .WithTags("Training")
            .WithSummary("Создать SQL-задание")
            .WithDescription(
                "Создаёт новое задание тренажёра. " +
                "Возвращает 201 Created с телом ответа. " +
                "400 — не прошла валидация. " +
                "409 — TargetDb, тема или SQL-запрос не найдены.")
            .Produces<SqlTaskResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
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
