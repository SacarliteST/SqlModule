using SQLModule.Contracts.Training.SqlTask;

namespace SQLModule.Client.SqlTask;

/// <summary>Клиент для работы с SQL-заданиями тренажёра.</summary>
public interface ISqlTaskClient : ICrudClient<CreateSqlTaskRequest, UpdateSqlTaskRequest, SqlTaskResponse>
{
    /// <summary>Получить агрегированные детали задания для преподавателя.</summary>
    Task<TeacherTaskDetailsResponse?> GetTeacherDetailsAsync(
        Guid taskId,
        CancellationToken ct = default);

    /// <summary>Опубликовать подготовленное SQL-задание.</summary>
    Task<SqlTaskResponse> PublishAsync(Guid taskId, CancellationToken ct = default);

    /// <summary>Архивировать SQL-задание.</summary>
    Task<SqlTaskResponse> ArchiveAsync(Guid taskId, CancellationToken ct = default);

    /// <summary>Обновить эталонное решение подготовленного SQL-задания.</summary>
    Task<UpdateTaskReferenceQueryResponse> UpdateReferenceQueryAsync(
        Guid taskId,
        UpdateTaskReferenceQueryRequest request,
        CancellationToken ct = default);
}
