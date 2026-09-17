namespace SQLModule.Domain.Training;

/// <summary>Состояние прохождения SQL-задания.</summary>
public enum ProgressStatus
{
    Active,
    Finalizing,
    CompletionPending,
    CompletionFailed,
    Completed,
    Expired
}
