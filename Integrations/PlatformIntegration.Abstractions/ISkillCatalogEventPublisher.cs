namespace SQLModule.PlatformIntegration.Abstractions;

/// <summary>Публикует изменения каталога навыков в Kafka.</summary>
public interface ISkillCatalogEventPublisher
{
    Task PublishAsync(
        string topic,
        string messageKey,
        string messageJson,
        CancellationToken ct = default);
}
