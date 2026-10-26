namespace SQLModule.Contracts.Training.Student;

/// <summary>Фильтры личной истории попыток.</summary>
public sealed record StudentAttemptsRequest(
    Guid? TaskId = null,
    Guid? TopicId = null,
    ExecutionStatus? Status = null,
    bool? IsCorrect = null,
    DateTimeOffset? DateFrom = null,
    DateTimeOffset? DateTo = null,
    int Offset = 0,
    int Limit = 20);
