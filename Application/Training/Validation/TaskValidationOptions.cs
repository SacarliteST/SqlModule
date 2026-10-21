namespace SQLModule.Application.Training.Validation;

public sealed class TaskValidationOptions
{
    public const string SectionKey = "TaskValidation";

    public int MaxAttemptsLimit { get; init; } = 100;
}
