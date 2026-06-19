namespace SQLModule.Contracts.Training.Attempt;

/// <summary>Запрос на создание попытки выполнения задания.</summary>
/// <param name="IsSuccess">Признак успешного выполнения задания.</param>
/// <param name="StartAttempt">Дата и время начала попытки.</param>
/// <param name="EndAttempt">Дата и время завершения попытки (должно быть >= StartAttempt).</param>
/// <param name="TaskId">Идентификатор выполняемого задания.</param>
/// <param name="QueryId">Идентификатор SQL-запроса, отправленного студентом.</param>
public record CreateAttemptRequest(
    bool IsSuccess,
    DateTimeOffset StartAttempt,
    DateTimeOffset EndAttempt,
    Guid TaskId,
    Guid QueryId);
