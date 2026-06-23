using SQLModule.Domain.Training;

namespace SQLModule.Contracts.Training.Attempt;

/// <summary>Результат проверки попытки выполнения задания.</summary>
public record SubmitAttemptResponse(
    Guid AttemptId,
    ExecutionStatus Status,
    bool IsCorrect,
    CheckReason Reason,
    int? RowCount,
    long? DurationMs,
    string? ErrorMessage,
    IReadOnlyList<string> ActualColumns,
    IReadOnlyList<IReadOnlyList<string?>> ActualRows);
