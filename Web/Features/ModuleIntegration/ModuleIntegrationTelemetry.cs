using System.Diagnostics;
using System.Diagnostics.Metrics;
using SQLModule.Domain.ModuleIntegration;

namespace SQLModule.Web.Features.ModuleIntegration;

internal static class ModuleIntegrationTelemetry
{
    internal const string InstrumentationName = "SQLModule.ModuleIntegration";

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
    private static readonly Counter<long> CompletionCounter = Meter.CreateCounter<long>(
        "sqlmodule.module_integration.completion.count");
    private static long EventPending;
    private static long GradePending;

    private static readonly ObservableGauge<long> PendingMessagesGauge = Meter.CreateObservableGauge(
        "sqlmodule_integration_pending_messages",
        ObservePendingMessages);

    internal static Activity? StartActivity(string name) =>
        ActivitySource.StartActivity(name, ActivityKind.Internal);

    internal static void RecordPush(string outcome) =>
        PushCounter.Add(1, new KeyValuePair<string, object?>("outcome", outcome));

    internal static void RecordCurrent(string outcome) =>
        CurrentCounter.Add(1, new KeyValuePair<string, object?>("outcome", outcome));

    internal static void RecordPublish(PendingPublishKind kind, string outcome)
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

    internal static void RecordCompletion(string outcome) =>
        CompletionCounter.Add(1, new KeyValuePair<string, object?>("outcome", outcome));

    internal static void RecordPendingMessages(long eventCount, long gradeCount)
    {
        Interlocked.Exchange(ref EventPending, eventCount);
        Interlocked.Exchange(ref GradePending, gradeCount);
    }

    internal static void RecordSessionTransition(
        ModuleSessionStatus from,
        ModuleSessionStatus to,
        long count = 1) =>
        SessionTransitionsCounter.Add(
            count,
            new KeyValuePair<string, object?>("from", ToMetricName(from)),
            new KeyValuePair<string, object?>("to", ToMetricName(to)));

    internal static void RecordExpiredSessions(long count) =>
        ExpiredSessionsCounter.Add(count);

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

internal static class ModuleIntegrationTelemetryOutcomes
{
    internal const string Created = "created";
    internal const string Updated = "updated";
    internal const string Found = "found";
    internal const string NotFound = "not_found";
    internal const string Unauthorized = "unauthorized";
    internal const string Conflict = "conflict";
    internal const string Sent = "sent";
    internal const string Retry = "retry";
    internal const string DeadLetter = "dead_letter";
    internal const string Accepted = "accepted";
    internal const string Rejected = "rejected";
}
