using System.Diagnostics;
using EducationSPD.Kafka.Contracts;
using EducationSPD.Kafka.Contracts.V1;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SQLModule.Application.Persistence;
using SQLModule.Domain.ModuleIntegration;
using SQLModule.Domain.Training;
using ContractSkillOutcome = EducationSPD.Kafka.Contracts.V1.SkillOutcome;
using ContractSkillResult = EducationSPD.Kafka.Contracts.V1.SkillResult;
using ContractSkillResultSource = EducationSPD.Kafka.Contracts.V1.SkillResultSource;

namespace SQLModule.Application.SpdIntegration;

/// <summary>Атомарно добавляет итог учебного эпизода в общий integration outbox.</summary>
public sealed class LearningEpisodeOutbox(
    IApplicationDbContext db,
    IOptions<EducationSpdOptions> options,
    ILogger<LearningEpisodeOutbox> logger)
{
    public void Add(
        LearningEpisode episode,
        IReadOnlyCollection<LearningEpisodeSkillResult> results)
    {
        if (!options.Value.Enabled || results.Count == 0)
        {
            return;
        }

        var eventId = Guid.NewGuid();
        var finalizedAt = episode.FinalizedAt ?? throw new InvalidOperationException(
            "Для события наблюдения эпизод должен быть финализирован.");
        var message = new LearningEpisodeFinalized(
            1,
            eventId,
            episode.Id,
            episode.StudentId,
            options.Value.ModuleKey,
            episode.TaskId,
            finalizedAt,
            results.Select(Map).ToArray());
        var activity = Activity.Current;
        db.PendingPublishes.Add(PendingPublish.CreateKafka(
            eventId,
            PendingPublishKind.LearningEpisode,
            episode.Id,
            $"episode:{episode.Id:D}",
            options.Value.Kafka.EpisodeFinalizedTopic,
            $"{options.Value.ModuleKey}:{episode.StudentId:D}",
            KafkaContractJson.Serialize(message),
            finalizedAt,
            activity?.Id,
            activity?.TraceStateString));
        logger.LogInformation(
            "Для учебного эпизода {EpisodeId} создано outbox-событие {EventId}",
            episode.Id,
            eventId);
    }

    private static ContractSkillResult Map(LearningEpisodeSkillResult result) => new(
        result.SkillExternalId,
        result.Source == LearningEpisodeSkillSource.ValidationRule
            ? ContractSkillResultSource.ValidationRule
            : ContractSkillResultSource.TaskTag,
        result.Outcome == LearningEpisodeSkillOutcome.Succeeded
            ? ContractSkillOutcome.Succeeded
            : ContractSkillOutcome.Failed);
}
