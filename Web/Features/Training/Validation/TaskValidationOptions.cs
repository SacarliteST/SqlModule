namespace SQLModule.Web.Features.Training.Validation;

internal sealed class TaskValidationOptions
{
    internal const string SectionKey = "TaskValidation";

    public int MaxAttemptsLimit { get; init; } = 100;
}
