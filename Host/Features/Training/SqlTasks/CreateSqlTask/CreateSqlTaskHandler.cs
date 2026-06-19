using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts.Training.SqlTask;
using SQLModule.Data.Core;
using SQLModule.Domain.Training;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Training.SqlTasks;

internal record CreateSqlTaskCommand(
    Guid TargetDbId, Guid TopicId, Guid SqlQueryId,
    string TaskName, string TaskText, short DifficultyLevel)
    : IRequest<Result<SqlTaskResponse>>;

internal sealed class CreateSqlTaskHandler(AppDbContext db)
    : IRequestHandler<CreateSqlTaskCommand, Result<SqlTaskResponse>>
{
    public async Task<Result<SqlTaskResponse>> Handle(CreateSqlTaskCommand command, CancellationToken ct)
    {
        if (!await db.TargetDbs.AnyAsync(x => x.Id == command.TargetDbId, ct))
        {
            return Result<SqlTaskResponse>.Fail(Error.Conflict(
                "SqlTask.TargetDbNotFound",
                $"Целевая БД с id '{command.TargetDbId}' не найдена."));
        }

        if (!await db.Topics.AnyAsync(x => x.Id == command.TopicId, ct))
        {
            return Result<SqlTaskResponse>.Fail(Error.Conflict(
                "SqlTask.TopicNotFound",
                $"Тема с id '{command.TopicId}' не найдена."));
        }

        if (!await db.SqlQueries.AnyAsync(x => x.Id == command.SqlQueryId, ct))
        {
            return Result<SqlTaskResponse>.Fail(Error.Conflict(
                "SqlTask.QueryNotFound",
                $"SQL-запрос с id '{command.SqlQueryId}' не найден."));
        }

        var entity = SqlTask.Create(
            command.TargetDbId, command.TopicId, command.SqlQueryId,
            command.TaskName, command.TaskText, command.DifficultyLevel);

        db.SqlTasks.Add(entity);
        await db.SaveChangesAsync(ct);
        return SqlTaskMappings.ToResponse(entity);
    }
}
