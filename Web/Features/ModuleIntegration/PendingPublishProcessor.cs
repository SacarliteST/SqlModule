using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using SQLModule.Data.Core;
using SQLModule.Domain.ModuleIntegration;
using SQLModule.PlatformIntegration.Abstractions;
using SQLModule.PlatformIntegration.Contracts;

namespace SQLModule.Web.Features.ModuleIntegration;

internal sealed class PendingPublishProcessor(
    AppDbContext db,
    IPracticeEventPublisher publisher,
    IEducationCompletionClient completionClient,
    IOptions<ModuleIntegrationOptions> options,
    TimeProvider timeProvider,
    ILogger<PendingPublishProcessor> logger)
{
    internal async Task<int> ProcessBatchAsync(CancellationToken ct)
    {
        var publisherOptions = options.Value.Publisher;
        var now = timeProvider.GetUtcNow();
        var messages = await db.PendingPublishes
            .Where(message =>
                message.SentAt == null &&
                message.DeadLetterAt == null &&
                message.NextAttemptAt <= now)
            .OrderBy(message => message.NextAttemptAt)
            .ThenBy(message => message.CreatedAt)
            .Take(publisherOptions.BatchSize)
            .ToListAsync(ct);

        foreach (var message in messages)
        {
            using var activity = ModuleIntegrationTelemetry.StartActivity("module-integration.publish");
            activity?.SetTag("messaging.operation.type", "publish");
            activity?.SetTag("sqlmodule.publish.kind", message.Kind.ToString());
            try
            {
                if (message.Kind == PendingPublishKind.Event)
                {
                    var request = JsonSerializer.Deserialize<PracticeEventMessage>(
                        message.MessageJson,
                        PlatformIntegrationJson.Default) ?? throw new JsonException(
                        "Outbox-сообщение события платформенной сессии не содержит тела запроса.");
                    await publisher.PublishAsync(request, ct);
                    message.MarkSent(timeProvider.GetUtcNow());
                    ModuleIntegrationTelemetry.RecordPublish(
                        message.Kind,
                        ModuleIntegrationTelemetryOutcomes.Sent);
                }
                else
                {
                    await ProcessGradeAsync(message, ct);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                var failedAt = timeProvider.GetUtcNow();
                var deliveryOptions = GetDeliveryOptions(message.Kind);
                var moveToDeadLetter = message.Attempts + 1 >= deliveryOptions.MaxAttempts;
                var exponentialFactor = 1L << Math.Min(message.Attempts, 30);
                var delaySeconds = Math.Min(
                    deliveryOptions.MaxRetryDelaySeconds,
                    deliveryOptions.InitialRetryDelaySeconds * exponentialFactor);
                message.RegisterFailure(
                    failedAt,
                    failedAt.AddSeconds(delaySeconds),
                    moveToDeadLetter);
                if (moveToDeadLetter && message.Kind == PendingPublishKind.Grade)
                {
                    await MarkCompletionFailedAsync(message.SessionId, ct);
                }
                ModuleIntegrationTelemetry.RecordPublish(
                    message.Kind,
                    moveToDeadLetter
                        ? ModuleIntegrationTelemetryOutcomes.DeadLetter
                        : ModuleIntegrationTelemetryOutcomes.Retry);
                logger.LogWarning(
                    "Не удалось отправить сообщение платформы {MessageId} типа {Kind} для сессии {SessionId}; попытка {Attempt} из {MaxAttempts}; тип ошибки {ExceptionType}",
                    message.Id,
                    message.Kind,
                    message.SessionId,
                    message.Attempts,
                    deliveryOptions.MaxAttempts,
                    exception.GetType().Name);
            }

            await db.SaveChangesAsync(ct);
        }

        var retentionBoundary = now.AddDays(-publisherOptions.SentRetentionDays);
        await db.PendingPublishes
            .Where(message => message.SentAt != null && message.SentAt < retentionBoundary)
            .ExecuteDeleteAsync(ct);
        var eventPending = await CountPendingAsync(PendingPublishKind.Event, ct);
        var gradePending = await CountPendingAsync(PendingPublishKind.Grade, ct);
        ModuleIntegrationTelemetry.RecordPendingMessages(eventPending, gradePending);
        return messages.Count;
    }

    private async Task ProcessGradeAsync(PendingPublish message, CancellationToken ct)
    {
        var deliveredAt = timeProvider.GetUtcNow();
        var request = JsonSerializer.Deserialize<PracticeCompletionRequest>(
            message.MessageJson,
            PlatformIntegrationJson.Default) ?? throw new JsonException(
            "Outbox-сообщение завершения платформенной сессии не содержит тела запроса.");
        var result = await completionClient.CompleteAsync(message.SessionId, request, ct);
        if (result is EducationCompletionDeliveryResult.Accepted or
            EducationCompletionDeliveryResult.TerminalConflict)
        {
            message.MarkSent(deliveredAt);
            var session = await db.ModuleSessions.SingleOrDefaultAsync(
                value => value.Id == message.SessionId, ct);
            if (session is not null && session.Status != ModuleSessionStatus.Completed)
            {
                var previousStatus = session.Status;
                session.MarkCompleted();
                ModuleIntegrationTelemetry.RecordSessionTransition(
                    previousStatus,
                    ModuleSessionStatus.Completed);
            }

            var progress = await db.StudentTaskProgresses.SingleOrDefaultAsync(
                value => value.ModuleSessionId == message.SessionId, ct);
            if (progress is not null && progress.Status != Domain.Training.ProgressStatus.Completed)
            {
                progress.MarkCompleted();
            }

            if (result == EducationCompletionDeliveryResult.TerminalConflict)
            {
                logger.LogWarning(
                    "Education окончательно отклонил оценку сессии {SessionId}; повтор не выполняется.",
                    message.SessionId);
            }

            ModuleIntegrationTelemetry.RecordCompletion(
                result == EducationCompletionDeliveryResult.Accepted
                    ? ModuleIntegrationTelemetryOutcomes.Accepted
                    : ModuleIntegrationTelemetryOutcomes.Conflict);
            ModuleIntegrationTelemetry.RecordPublish(
                message.Kind,
                ModuleIntegrationTelemetryOutcomes.Sent);

            return;
        }

        message.MarkDeadLetter(deliveredAt);
        await MarkCompletionFailedAsync(message.SessionId, ct);
        ModuleIntegrationTelemetry.RecordCompletion(ModuleIntegrationTelemetryOutcomes.Rejected);
        ModuleIntegrationTelemetry.RecordPublish(
            message.Kind,
            ModuleIntegrationTelemetryOutcomes.DeadLetter);
        logger.LogError(
            "Education отклонил авторизацию или контракт завершения сессии {SessionId}; повтор не выполняется.",
            message.SessionId);
    }

    private async Task MarkCompletionFailedAsync(Guid sessionId, CancellationToken ct)
    {
        var session = await db.ModuleSessions.SingleOrDefaultAsync(
            value => value.Id == sessionId, ct);
        if (session?.Status == ModuleSessionStatus.CompletionPending)
        {
            session.MarkCompletionFailed();
            ModuleIntegrationTelemetry.RecordSessionTransition(
                ModuleSessionStatus.CompletionPending,
                ModuleSessionStatus.CompletionFailed);
        }

        var progress = await db.StudentTaskProgresses.SingleOrDefaultAsync(
            value => value.ModuleSessionId == sessionId, ct);
        if (progress?.Status == Domain.Training.ProgressStatus.CompletionPending)
        {
            progress.MarkCompletionFailed();
        }
    }

    private Task<int> CountPendingAsync(PendingPublishKind kind, CancellationToken ct) =>
        db.PendingPublishes.CountAsync(
            message =>
                message.Kind == kind &&
                message.SentAt == null &&
                message.DeadLetterAt == null,
            ct);

    private DeliveryOptions GetDeliveryOptions(PendingPublishKind kind)
    {
        if (kind == PendingPublishKind.Event)
        {
            var kafka = options.Value.Kafka;
            return new DeliveryOptions(
                kafka.MaxAttempts,
                kafka.InitialRetryDelaySeconds,
                kafka.MaxRetryDelaySeconds);
        }

        var completion = options.Value.EducationCompletion;
        return new DeliveryOptions(
            completion.MaxAttempts,
            completion.InitialRetryDelaySeconds,
            completion.MaxRetryDelaySeconds);
    }

    private sealed record DeliveryOptions(
        int MaxAttempts,
        int InitialRetryDelaySeconds,
        int MaxRetryDelaySeconds);
}
