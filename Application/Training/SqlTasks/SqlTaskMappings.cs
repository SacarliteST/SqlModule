using SQLModule.Contracts.Training.SqlTask;
using SQLModule.Domain.Training;

namespace SQLModule.Application.Training.SqlTasks;

public static class SqlTaskMappings
{
    public static SqlTaskResponse ToResponse(SqlTask e) => new(
        e.Id, e.TopicId, e.SqlQueryId,
        e.TaskName, e.TaskText, e.DifficultyLevel, e.PublicationStatus,
        e.CreatedById, e.CreatedAt, e.UpdatedById, e.UpdatedAt,
        CreatedByName: e.CreatedByName, UpdatedByName: e.UpdatedByName);

    public static CreateSqlTaskCommand ToCommand(CreateSqlTaskRequest req) =>
        new(
            req.TopicId!.Value,
            req.TaskName!,
            req.TaskText!,
            req.DifficultyLevel!.Value,
            ToData(req.ReferenceQuery!));

    public static UpdateSqlTaskCommand ToCommand(Guid id, UpdateSqlTaskRequest req) =>
        new(
            id,
            req.TaskName!,
            req.TaskText!,
            req.DifficultyLevel!.Value,
            req.PublicationStatus,
            req.TopicId);

    private static ReferenceQueryData ToData(ReferenceQueryRequest req) =>
        new(
            req.TargetDbId!.Value,
            req.QueryText!,
            req.StrictColumnOrder!.Value,
            req.StrictRowOrder!.Value);
}
