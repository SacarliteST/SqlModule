namespace SQLModule.Contracts.Training.Student;

/// <summary>Безопасные данные задания для студента.</summary>
public sealed record StudentTaskDetailsResponse(
    Guid Id,
    Guid TopicId,
    string TopicName,
    string TaskName,
    string TaskText,
    short DifficultyLevel,
    string DbmsName,
    StudentExecutionLimitsResponse ExecutionLimits,
    global::SQLModule.Contracts.Training.Validation.StudentTaskValidationResponse? Validation = null,
    long? BankNumber = null);
