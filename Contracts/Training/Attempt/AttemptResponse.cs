namespace SQLModule.Contracts.Training.Attempt;

/// <summary>Ответ с данными попытки выполнения задания.</summary>
/// <param name="Id">Уникальный идентификатор попытки.</param>
/// <param name="UserId">Идентификатор студента, совершившего попытку.</param>
/// <param name="IsSuccess">Признак успешного выполнения задания.</param>
/// <param name="StartAttempt">Дата и время начала попытки.</param>
/// <param name="EndAttempt">Дата и время завершения попытки.</param>
/// <param name="TaskId">Идентификатор выполняемого задания.</param>
/// <param name="QueryId">Идентификатор SQL-запроса, отправленного студентом.</param>
/// <param name="CreatedById">Идентификатор пользователя, создавшего запись.</param>
/// <param name="CreatedAt">Дата и время создания записи.</param>
/// <param name="UpdatedById">Идентификатор пользователя, последним изменившего запись.</param>
/// <param name="UpdatedAt">Дата и время последнего изменения записи.</param>
public record AttemptResponse(
    Guid Id,
    Guid UserId,
    bool IsSuccess,
    DateTimeOffset StartAttempt,
    DateTimeOffset EndAttempt,
    Guid TaskId,
    Guid QueryId,
    Guid CreatedById,
    DateTimeOffset CreatedAt,
    Guid UpdatedById,
    DateTimeOffset UpdatedAt);
