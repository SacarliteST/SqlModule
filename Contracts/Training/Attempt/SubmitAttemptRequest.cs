namespace SQLModule.Contracts.Training.Attempt;

/// <summary>Запрос на отправку попытки выполнения задания.</summary>
public record SubmitAttemptRequest(Guid TaskId, string SubmittedSql);
