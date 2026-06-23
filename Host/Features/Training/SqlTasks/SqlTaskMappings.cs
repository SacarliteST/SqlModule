using SQLModule.Contracts.Training.SqlTask;
using SQLModule.Domain.Training;

namespace SQLModule.Host.Features.Training.SqlTasks;

internal static class SqlTaskMappings
{
    internal static SqlTaskResponse ToResponse(SqlTask e) => new(
        e.Id, e.TopicId, e.SqlQueryId,
        e.TaskName, e.TaskText, e.DifficultyLevel,
        e.CreatedById, e.CreatedAt, e.UpdatedById, e.UpdatedAt);

    internal static CreateSqlTaskCommand ToCommand(CreateSqlTaskRequest req) =>
        new(req.TopicId, req.SqlQueryId, req.TaskName, req.TaskText, req.DifficultyLevel);

    internal static UpdateSqlTaskCommand ToCommand(Guid id, UpdateSqlTaskRequest req) =>
        new(id, req.TaskName, req.TaskText, req.DifficultyLevel);
}
