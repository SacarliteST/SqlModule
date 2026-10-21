using SQLModule.Domain.Training;

namespace SQLModule.Application.Training.Attempts.SubmitAttempt;

/// <summary>Преобразования попытки в формат outbox-события для платформы.</summary>
internal static class PracticeEventMapping
{
    /// <summary>Потребители события (Education) ожидают "SUCCESS"/"ERROR"/"TIMEOUT", а не имя enum.</summary>
    internal static string ToIntegrationStatus(ExecutionStatus status) => status switch
    {
        ExecutionStatus.Succeeded => "SUCCESS",
        ExecutionStatus.Error => "ERROR",
        ExecutionStatus.TimedOut => "TIMEOUT",
        _ => "UNKNOWN"
    };
}
