namespace SQLModule.Domain.Training;

/// <summary>Внутренняя причина окончательного закрытия учебного эпизода.</summary>
public enum LearningEpisodeFinalizationReason
{
    PerfectScore,
    AcceptedPartialResult,
    Abandoned,
    AttemptsExhausted,
    HeartbeatTimeout,
    LearningTimeExpired,
    TechnicalSessionExpired,
    InfrastructureFailure,
    Administrative
}
