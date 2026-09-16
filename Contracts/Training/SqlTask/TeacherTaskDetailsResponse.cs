using SQLModule.Domain.Training;

namespace SQLModule.Contracts.Training.SqlTask;

/// <summary>Агрегированная read-модель деталей задания для преподавателя.</summary>
/// <param name="TaskId">Идентификатор задания.</param>
/// <param name="TopicId">Идентификатор темы.</param>
/// <param name="TopicName">Название темы.</param>
/// <param name="TaskName">Название задания.</param>
/// <param name="Description">Условие задания.</param>
/// <param name="DifficultyLevel">Уровень сложности.</param>
/// <param name="PublicationStatus">Статус публикации.</param>
/// <param name="CreatedAt">Дата создания.</param>
/// <param name="UpdatedAt">Дата последнего изменения.</param>
/// <param name="CreatedById">Идентификатор автора.</param>
/// <param name="SqlQuery">Текущее эталонное решение.</param>
/// <param name="TargetDb">Учебная база эталонного решения.</param>
/// <param name="CanEditTask">
/// Можно ли изменять основные поля задания: название, условие и сложность.
/// Флаг не распространяется на тему, статус публикации и эталонное решение — для них действуют отдельные правила.
/// </param>
/// <param name="CanEditReferenceQuery">Можно ли сейчас изменить эталонное решение отдельной операцией.</param>
/// <param name="ReferenceQueryEditRestriction">Понятная пользователю причина запрета изменения эталона; null, если изменение разрешено.</param>
/// <param name="AttemptsCount">Общее количество попыток.</param>
/// <param name="LastAttempts">Последние попытки выполнения.</param>
/// <param name="CanPublish">Можно ли опубликовать задание сейчас.</param>
/// <param name="CanArchive">Можно ли архивировать задание сейчас.</param>
/// <param name="CanDelete">Можно ли удалить задание сейчас.</param>
/// <param name="LifecycleRestriction">Причина запрета lifecycle-операции, если она общая.</param>
/// <param name="CreatedByName">Отображаемое имя автора на момент создания задания.</param>
public sealed record TeacherTaskDetailsResponse(
    Guid TaskId,
    Guid TopicId,
    string TopicName,
    string TaskName,
    string Description,
    short DifficultyLevel,
    PublicationStatus PublicationStatus,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    Guid CreatedById,
    TeacherTaskSqlQueryResponse SqlQuery,
    TeacherTaskTargetDbResponse TargetDb,
    bool CanEditTask,
    bool CanEditReferenceQuery,
    string? ReferenceQueryEditRestriction,
    int AttemptsCount,
    IReadOnlyList<TeacherTaskAttemptResponse> LastAttempts,
    bool CanPublish = false,
    bool CanArchive = false,
    bool CanDelete = false,
    string? LifecycleRestriction = null,
    string? CreatedByName = null);

/// <summary>Эталонный запрос в деталях задания.</summary>
public sealed record TeacherTaskSqlQueryResponse(
    Guid SqlQueryId,
    string SqlText,
    bool IsRequiredColumnOrder,
    bool IsRequiredRowOrder);

/// <summary>Учебная база в деталях задания.</summary>
public sealed record TeacherTaskTargetDbResponse(
    Guid TargetDbId,
    string DbName,
    string DbmsName,
    IReadOnlyList<TeacherTaskTableResponse> Tables);

/// <summary>Краткое описание таблицы учебной базы.</summary>
public sealed record TeacherTaskTableResponse(string TableName, int ColumnsCount);

/// <summary>Краткая попытка студента.</summary>
public sealed record TeacherTaskAttemptResponse(
    Guid AttemptId,
    Guid StudentId,
    string StudentName,
    bool IsCorrect,
    ExecutionStatus Status,
    long? DurationMs,
    DateTimeOffset FinishedAt,
    int? AttemptNumber = null,
    int? Score = null);
