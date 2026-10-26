namespace SQLModule.Contracts.Training.Validation;

/// <summary>Состояние платформенной сессии; отсутствует в standalone-режиме.</summary>
public sealed record StudentSessionStateResponse(
    SessionStatus Status,
    int BestScore,
    int AttemptsUsed,
    int? MaxAttempts,
    int? AttemptsRemaining,
    bool CanSubmit,
    bool CanFinalize,
    bool IsPassed,
    DateTimeOffset? ExpiresAt,
    int? FinalScore,
    FinalizationReason? FinalizationReason,
    DateTimeOffset? FinalizedAt);
