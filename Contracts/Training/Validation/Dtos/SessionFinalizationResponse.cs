namespace SQLModule.Contracts.Training.Validation;

/// <summary>Результат ручного завершения платформенной сессии.</summary>
public sealed record SessionFinalizationResponse(
    SessionStatus Status,
    int BestScore,
    int FinalScore,
    bool IsPassed,
    FinalizationReason Reason,
    DateTimeOffset FinalizedAt,
    bool CanReturnToEducation,
    string? ReturnUrl);
