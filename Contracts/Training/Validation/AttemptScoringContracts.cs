using SQLModule.Domain.Training;

namespace SQLModule.Contracts.Training.Validation;

/// <summary>Безопасный результат одного критерия в попытке.</summary>
public sealed record AttemptCheckResultResponse(
    ValidationCheckKind Kind,
    ValidationCheckStatus Status,
    int Weight,
    int AwardedScore,
    string? Message);

/// <summary>Подсказка, разрешённая текущей validation version.</summary>
public sealed record AttemptHintResponse(HintGroup Group, string Message);

/// <summary>Составная оценка попытки и актуальное состояние progress.</summary>
public sealed record AttemptScoringResponse(
    Guid AttemptId,
    Guid ProgressId,
    Guid ValidationVersionId,
    int AttemptNumber,
    int Score,
    int BestScore,
    int PassingScore,
    bool IsPassed,
    int AttemptsUsed,
    int? AttemptsRemaining,
    bool CanSubmit,
    bool CanFinalize,
    IReadOnlyList<AttemptCheckResultResponse> Checks,
    IReadOnlyList<AttemptHintResponse> Hints);
