using SQLModule.Domain.Common;

namespace SQLModule.Domain.Training;

/// <summary>Сохранённый результат одного критерия конкретной попытки.</summary>
public sealed class AttemptCheckResult : BaseEntity
{
    public Guid AttemptId { get; private set; }
    public Guid ValidationCheckId { get; private set; }
    public ValidationCheckKind Kind { get; private set; }
    public ValidationCheckStatus Status { get; private set; }
    public int Weight { get; private set; }
    public int AwardedScore { get; private set; }
    public int Order { get; private set; }
    public string? Message { get; private set; }
    public string? DiagnosticJson { get; private set; }

    public Attempt Attempt { get; private set; } = null!;

    private AttemptCheckResult(
        Guid id,
        Guid attemptId,
        Guid validationCheckId,
        ValidationCheckKind kind,
        ValidationCheckStatus status,
        int weight,
        int awardedScore,
        int order,
        string? message,
        string? diagnosticJson) : base(id)
    {
        AttemptId = attemptId;
        ValidationCheckId = validationCheckId;
        Kind = kind;
        Status = status;
        Weight = weight;
        AwardedScore = awardedScore;
        Order = order;
        Message = message;
        DiagnosticJson = diagnosticJson;
    }

    public static AttemptCheckResult Create(
        Guid attemptId,
        Guid validationCheckId,
        ValidationCheckKind kind,
        ValidationCheckStatus status,
        int weight,
        int awardedScore,
        int order,
        string? message,
        string? diagnosticJson,
        Guid? id = null) =>
        new(
            id ?? Guid.NewGuid(), attemptId, validationCheckId, kind, status,
            weight, awardedScore, order, message, diagnosticJson);
}
