using System.Text.Json;
using Microsoft.Extensions.Options;
using SQLModule.Application.Persistence;
using SQLModule.Domain.ModuleIntegration;
using SQLModule.Domain.Training;

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
        var message = new
        {
            schemaVersion = 1,
            eventId,
            moduleKey = options.Value.ModuleKey,
            skillId = skill.SkillId,
            name = skill.Name,
            description = skill.Description,
            origin = skill.Origin.ToString(),
            isDeleted = skill.IsDeleted,
            occurredAt
        };
        var json = JsonSerializer.Serialize(message, JsonSerializerOptions.Web);
        db.PendingPublishes.Add(PendingPublish.CreateKafka(
            eventId,
            PendingPublishKind.SkillCatalog,
            skill.Id,
            $"skill:{skill.Id:D}:{eventId:D}",
            options.Value.Kafka.SkillCatalogTopic,
            $"{options.Value.ModuleKey}:{skill.SkillId}",
            json,
            occurredAt));
    }
}
