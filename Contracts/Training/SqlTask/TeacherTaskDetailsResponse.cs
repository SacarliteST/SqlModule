using SQLModule.Domain.Training;

namespace SQLModule.Contracts.Training.SqlTask;

/// <summary>Агрегированная read-модель деталей задания для преподавателя.</summary>
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
    int AttemptsCount,
    IReadOnlyList<TeacherTaskAttemptResponse> LastAttempts);

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
    DateTimeOffset FinishedAt);
