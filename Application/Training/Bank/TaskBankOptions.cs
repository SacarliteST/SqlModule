namespace SQLModule.Application.Training.Bank;

/// <summary>Ограничения открытого банка заданий.</summary>
public sealed class TaskBankOptions
{
    public const string SectionKey = "TaskBank";

    public int HintMaxLength { get; init; } = 4000;
    public int SolutionExplanationMaxLength { get; init; } = 8000;
    public int SubmissionIntervalSeconds { get; init; } = 3;
    public int RetryAfterSeconds { get; init; } = 3;
}
