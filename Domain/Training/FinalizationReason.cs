namespace SQLModule.Domain.Training;

/// <summary>Причина завершения платформенной сессии.</summary>
public enum FinalizationReason
{
    Manual,
    PerfectScore,
    AttemptsExhausted,
    Expired,
    EducationClosed
}
