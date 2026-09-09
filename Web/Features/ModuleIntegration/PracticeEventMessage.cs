using System.Text.Json.Serialization;

namespace SQLModule.Web.Features.ModuleIntegration;

internal sealed record PracticeEventMessage(
    Guid SessionId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] string SessionKey,
    Guid EventId,
    string Kind,
    DateTimeOffset OccurredAt,
    PracticeEventPayload Payload);

internal sealed record PracticeEventPayload(
    string SubmittedSql,
    string Status,
    int? RowCount,
    long? DurationMs,
    bool IsCorrect,
    string Reason);
