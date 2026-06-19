namespace SQLModule.Contracts.Training.Attempt;

/// <summary>Запрос на обновление результата попытки.</summary>
/// <param name="IsSuccess">Новый признак успешного выполнения задания.</param>
/// <param name="EndAttempt">Новое время завершения попытки (должно быть >= времени начала).</param>
public record UpdateAttemptRequest(bool IsSuccess, DateTimeOffset EndAttempt);
