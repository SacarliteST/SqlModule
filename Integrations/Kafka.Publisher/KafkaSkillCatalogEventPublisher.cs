using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SQLModule.PlatformIntegration.Abstractions;

namespace SQLModule.Kafka.Publisher;

internal sealed class KafkaSkillCatalogEventPublisher(
    IOptions<SkillCatalogKafkaPublisherOptions> options,
    ILogger<KafkaSkillCatalogEventPublisher> logger) : ISkillCatalogEventPublisher, IDisposable
{
    private readonly IProducer<string, string> producer =
        new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            EnableIdempotence = true,
            Acks = Acks.All,
        }).Build();

    public async Task PublishAsync(
        string topic,
        string messageKey,
        string messageJson,
        CancellationToken ct = default)
    {
        var result = await producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = messageKey,
            Value = messageJson,
        }, ct);
        logger.LogInformation(
            "Изменение каталога навыков опубликовано в Kafka; ключ {MessageKey}, топик {Topic}, партиция {Partition}, смещение {Offset}",
            messageKey,
            result.Topic,
            result.Partition.Value,
            result.Offset.Value);
    }

    public void Dispose() => producer.Dispose();
}
