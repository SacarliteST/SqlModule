using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Http;
using Shouldly;
using SQLModule.Domain.ModuleIntegration;
using SQLModule.Web.Common;
using SQLModule.Web.Features.ModuleIntegration;

namespace SQLModule.UnitTests.Common;

public sealed class ModuleIntegrationObservabilityTests
{
    [Fact(DisplayName = "ProblemDetails содержит trace ID и не раскрывает текст внутренней ошибки")]
    public void ProblemDetails_UsesTraceIdAndSafeInternalDetail()
    {
        using var activity = new Activity("problem-test").Start();
        const string secret = "secret-session-key";

        var problem = ApiProblemFactory.Create(
            StatusCodes.Status500InternalServerError,
            "Внутренняя ошибка",
            ExceptionHandlerExtensions.GetSafeDetail(StatusCodes.Status500InternalServerError),
            "Unhandled.InternalServerError");

        problem.Extensions["traceId"].ShouldBe(activity.Id);
        problem.Detail.ShouldNotBeNull();
        problem.Detail.ShouldNotContain(secret);
        problem.Detail.ShouldBe("При обработке запроса произошла внутренняя ошибка.");
    }

    [Fact(DisplayName = "Integration-метрики публикуют только низкокардинальные признаки результата")]
    public void Metrics_RecordOperationOutcomeWithoutIdentifiers()
    {
        var measurements = new List<RecordedMeasurement>();
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == ModuleIntegrationTelemetry.InstrumentationName)
                {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
            measurements.Add(new RecordedMeasurement(
                instrument.Name,
                value,
                tags.ToArray().ToDictionary(tag => tag.Key, tag => tag.Value as string))));
        listener.Start();

        ModuleIntegrationTelemetry.RecordPush(ModuleIntegrationTelemetryOutcomes.Created);
        ModuleIntegrationTelemetry.RecordCurrent(ModuleIntegrationTelemetryOutcomes.NotFound);
        ModuleIntegrationTelemetry.RecordCompletion(ModuleIntegrationTelemetryOutcomes.Accepted);
        ModuleIntegrationTelemetry.RecordPublish(
            PendingPublishKind.Event,
            ModuleIntegrationTelemetryOutcomes.Retry);
        ModuleIntegrationTelemetry.RecordPublish(
            PendingPublishKind.Grade,
            ModuleIntegrationTelemetryOutcomes.DeadLetter);
        ModuleIntegrationTelemetry.RecordSessionTransition(
            ModuleSessionStatus.Active,
            ModuleSessionStatus.CompletionPending);
        ModuleIntegrationTelemetry.RecordExpiredSessions(2);
        ModuleIntegrationTelemetry.RecordPendingMessages(3, 1);
        listener.RecordObservableInstruments();

        measurements.ShouldContain(value =>
            value.Instrument == "sqlmodule_integration_publish_total" &&
            value.Tags["kind"] == "event" &&
            value.Tags["outcome"] == ModuleIntegrationTelemetryOutcomes.Retry);
        measurements.ShouldContain(value =>
            value.Instrument == "sqlmodule_integration_publish_retries_total" &&
            value.Tags["kind"] == "event");
        measurements.ShouldContain(value =>
            value.Instrument == "sqlmodule_integration_dead_letters_total" &&
            value.Tags["kind"] == "grade");
        measurements.ShouldContain(value =>
            value.Instrument == "sqlmodule_integration_session_transitions_total" &&
            value.Tags["from"] == "active" &&
            value.Tags["to"] == "completion_pending");
        measurements.ShouldContain(value =>
            value.Instrument == "sqlmodule_integration_expired_sessions_total" &&
            value.Value == 2);
        measurements.ShouldContain(value =>
            value.Instrument == "sqlmodule_integration_pending_messages" &&
            value.Tags["kind"] == "event" &&
            value.Value == 3);
        measurements.ShouldContain(value =>
            value.Instrument == "sqlmodule_integration_pending_messages" &&
            value.Tags["kind"] == "grade" &&
            value.Value == 1);

        var allowedTagNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "kind", "outcome", "from", "to"
        };
        measurements
            .Where(value => value.Instrument.StartsWith("sqlmodule_integration_", StringComparison.Ordinal))
            .SelectMany(value => value.Tags.Keys)
            .ShouldAllBe(key => allowedTagNames.Contains(key));
    }

    private sealed record RecordedMeasurement(
        string Instrument,
        long Value,
        IReadOnlyDictionary<string, string?> Tags);
}
