using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SQLModule.PlatformIntegration.Abstractions;
using SQLModule.PlatformIntegration.Contracts;

namespace SQLModule.Kafka.Publisher;

internal sealed class KafkaPracticeEventPublisher(
    IOptions<KafkaPublisherOptions> options,
    ILogger<KafkaPracticeEventPublisher> logger) : IPracticeEventPublisher, IDisposable
{
    private readonly IProducer<string, string> producer =
        new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            EnableIdempotence = true,
            Acks = Acks.All,
        }).Build();

    public async Task PublishAsync(Guid sessionId, string messageJson, CancellationToken ct = default)
    {
        var result = await producer.ProduceAsync(options.Value.EventsTopic, new Message<string, string>
        {
            Key = sessionId.ToString("D"),
            Value = messageJson,
        }, ct);
        logger.LogInformation(
            "Событие платформенной сессии {SessionId} опубликовано в Kafka; топик {Topic}, партиция {Partition}, смещение {Offset}",
            sessionId,
            result.Topic,
            result.Partition.Value,
            result.Offset.Value);
    }

    public void Dispose() => producer.Dispose();
}
