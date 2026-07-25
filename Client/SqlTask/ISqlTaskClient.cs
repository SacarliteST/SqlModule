using SQLModule.Contracts.Training.SqlTask;

namespace SQLModule.Client.SqlTask;

/// <summary>Клиент для работы с SQL-заданиями тренажёра.</summary>
public interface ISqlTaskClient : ICrudClient<CreateSqlTaskRequest, UpdateSqlTaskRequest, SqlTaskResponse>
{
    /// <summary>Получить агрегированные детали задания для преподавателя.</summary>
    Task<TeacherTaskDetailsResponse?> GetTeacherDetailsAsync(
        Guid taskId,
        CancellationToken ct = default);
}
