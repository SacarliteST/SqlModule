using System.Text.Json;
using System.Text.Json.Serialization;

namespace SQLModule.PlatformIntegration.Contracts;

public sealed record PracticeEventMessage(
    Guid SessionId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] string SessionKey,
    Guid EventId,
    string Kind,
    DateTimeOffset OccurredAt,
    JsonElement Payload,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Markdown = null);

public sealed record PracticeEventPayload(
    string SubmittedSql,
    string Status,
    int? RowCount,
    long? DurationMs,
    bool IsCorrect,
    string Reason);

public sealed record PracticeSessionSummaryPayload(
    int TotalAttempts,
    int FinalGrade,
    string Reason,
    long? DurationMs);
