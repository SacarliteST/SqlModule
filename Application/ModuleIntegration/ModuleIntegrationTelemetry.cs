using System.Diagnostics;
using System.Diagnostics.Metrics;
using SQLModule.Domain.ModuleIntegration;

namespace SQLModule.Application.ModuleIntegration;

public static class ModuleIntegrationTelemetry
{
    public const string InstrumentationName = "SQLModule.ModuleIntegration";

    private static readonly ActivitySource ActivitySource = new(InstrumentationName);
    private static readonly Meter Meter = new(InstrumentationName);
    private static readonly Counter<long> PushCounter = Meter.CreateCounter<long>(
        "sqlmodule.module_sessions.push.count");
    private static readonly Counter<long> CurrentCounter = Meter.CreateCounter<long>(
        "sqlmodule.module_sessions.current.count");
    private static readonly Counter<long> PublishCounter = Meter.CreateCounter<long>(
        "sqlmodule_integration_publish_total");
    private static readonly Counter<long> PublishRetriesCounter = Meter.CreateCounter<long>(
        "sqlmodule_integration_publish_retries_total");
    private static readonly Counter<long> DeadLettersCounter = Meter.CreateCounter<long>(
        "sqlmodule_integration_dead_letters_total");
    private static readonly Counter<long> SessionTransitionsCounter = Meter.CreateCounter<long>(
        "sqlmodule_integration_session_transitions_total");
    private static readonly Counter<long> ExpiredSessionsCounter = Meter.CreateCounter<long>(
        "sqlmodule_integration_expired_sessions_total");
    private static readonly Counter<long> ExpiredSessionsWithAttemptsCounter = Meter.CreateCounter<long>(
        "sqlmodule_integration_expired_sessions_with_attempts_total");
    private static readonly Counter<long> CompletionCounter = Meter.CreateCounter<long>(
        "sqlmodule.module_integration.completion.count");
    private static long EventPending;
    private static long GradePending;

    private static readonly ObservableGauge<long> PendingMessagesGauge = Meter.CreateObservableGauge(
        "sqlmodule_integration_pending_messages",
        ObservePendingMessages);

    public static Activity? StartActivity(string name) =>
        ActivitySource.StartActivity(name, ActivityKind.Internal);

    public static void RecordPush(string outcome) =>
        PushCounter.Add(1, new KeyValuePair<string, object?>("outcome", outcome));

    public static void RecordCurrent(string outcome) =>
        CurrentCounter.Add(1, new KeyValuePair<string, object?>("outcome", outcome));

    public static void RecordPublish(PendingPublishKind kind, string outcome)
    {
        var kindName = ToMetricName(kind);
        PublishCounter.Add(
            1,
            new KeyValuePair<string, object?>("kind", kindName),
            new KeyValuePair<string, object?>("outcome", outcome));
        if (outcome == ModuleIntegrationTelemetryOutcomes.Retry)
        {
            PublishRetriesCounter.Add(1, new KeyValuePair<string, object?>("kind", kindName));
        }
        else if (outcome == ModuleIntegrationTelemetryOutcomes.DeadLetter)
        {
            DeadLettersCounter.Add(1, new KeyValuePair<string, object?>("kind", kindName));
        }
    }

    public static void RecordCompletion(string outcome) =>
        CompletionCounter.Add(1, new KeyValuePair<string, object?>("outcome", outcome));

    public static void RecordPendingMessages(long eventCount, long gradeCount)
    {
        Interlocked.Exchange(ref EventPending, eventCount);
        Interlocked.Exchange(ref GradePending, gradeCount);
    }

    public static void RecordSessionTransition(
        ModuleSessionStatus from,
        ModuleSessionStatus to,
        long count = 1) =>
        SessionTransitionsCounter.Add(
            count,
            new KeyValuePair<string, object?>("from", ToMetricName(from)),
            new KeyValuePair<string, object?>("to", ToMetricName(to)));

    public static void RecordExpiredSessions(long count) =>
        ExpiredSessionsCounter.Add(count);

    /// <summary>Сколько истёкших сессий имели хотя бы одну попытку студента —
    /// то есть потенциальную потерю результата (см. LogExpiredSessionsWithAttemptsAsync).</summary>
    public static void RecordExpiredSessionsWithAttempts(long count) =>
        ExpiredSessionsWithAttemptsCounter.Add(count);

    private static IEnumerable<Measurement<long>> ObservePendingMessages()
    {
        yield return new Measurement<long>(
            Interlocked.Read(ref EventPending),
            new KeyValuePair<string, object?>("kind", "event"));
        yield return new Measurement<long>(
            Interlocked.Read(ref GradePending),
            new KeyValuePair<string, object?>("kind", "grade"));
    }

    private static string ToMetricName(PendingPublishKind kind) => kind switch
    {
        PendingPublishKind.Event => "event",
        PendingPublishKind.Grade => "grade",
        _ => "unknown"
    };

    private static string ToMetricName(ModuleSessionStatus status) => status switch
    {
        ModuleSessionStatus.Active => "active",
        ModuleSessionStatus.CompletionPending => "completion_pending",
        ModuleSessionStatus.Completed => "completed",
        ModuleSessionStatus.CompletionFailed => "completion_failed",
        ModuleSessionStatus.Expired => "expired",
        _ => "unknown"
    };
}

public static class ModuleIntegrationTelemetryOutcomes
{
    public const string Created = "created";
    public const string Updated = "updated";
    public const string Found = "found";
    public const string NotFound = "not_found";
    public const string Unauthorized = "unauthorized";
    public const string Conflict = "conflict";
    public const string Sent = "sent";
    public const string Retry = "retry";
    public const string DeadLetter = "dead_letter";
    public const string Accepted = "accepted";
    public const string Rejected = "rejected";
}
