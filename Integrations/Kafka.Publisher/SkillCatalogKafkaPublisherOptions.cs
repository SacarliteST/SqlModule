namespace SQLModule.Kafka.Publisher;

internal sealed class SkillCatalogKafkaPublisherOptions
{
    public string BootstrapServers { get; init; } = String.Empty;
}
