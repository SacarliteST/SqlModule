namespace SQLModule.Domain.Training;

/// <summary>Причина завершения прохождения.</summary>
public enum FinalizationReason
{
    Manual,
    PerfectScore,
    AttemptsExhausted,
    Expired,
    EducationClosed,
    Restarted
}
