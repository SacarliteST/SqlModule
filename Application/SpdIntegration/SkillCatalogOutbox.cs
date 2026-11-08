using System.Diagnostics;
using EducationSPD.Kafka.Contracts;
using EducationSPD.Kafka.Contracts.V1;
using Microsoft.Extensions.Options;
using SQLModule.Application.Persistence;
using SQLModule.Domain.ModuleIntegration;
using SQLModule.Domain.Training;
using ContractSkillOrigin = EducationSPD.Kafka.Contracts.V1.SkillOrigin;

namespace SQLModule.Application.SpdIntegration;

/// <summary>Создаёт сообщение каталога навыков в общем integration outbox.</summary>
public sealed class SkillCatalogOutbox(
    IApplicationDbContext db,
    IOptions<EducationSpdOptions> options,
    TimeProvider timeProvider)
{
    public void Add(Skill skill)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        var occurredAt = timeProvider.GetUtcNow();
        var eventId = Guid.NewGuid();
        var message = new SkillCatalogChanged(
            1,
            eventId,
            options.Value.ModuleKey,
            skill.SkillId,
            skill.Name,
            skill.Description,
            skill.Origin == SQLModule.Domain.Training.SkillOrigin.System
                ? ContractSkillOrigin.System
                : ContractSkillOrigin.TeacherDefined,
            skill.IsDeleted,
            occurredAt);
        var activity = Activity.Current;
        db.PendingPublishes.Add(PendingPublish.CreateKafka(
            eventId,
            PendingPublishKind.SkillCatalog,
            skill.Id,
            $"skill:{skill.Id:D}:{eventId:D}",
            options.Value.Kafka.SkillCatalogTopic,
            $"{options.Value.ModuleKey}:{skill.SkillId}",
            KafkaContractJson.Serialize(message),
            occurredAt,
            activity?.Id,
            activity?.TraceStateString));
    }
}
