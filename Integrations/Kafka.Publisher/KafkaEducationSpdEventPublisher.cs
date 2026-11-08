using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SQLModule.EducationSpd.Abstractions;

namespace SQLModule.Kafka.Publisher;

internal sealed class KafkaEducationSpdEventPublisher(
    IOptions<SkillCatalogKafkaPublisherOptions> options,
    ILogger<KafkaEducationSpdEventPublisher> logger) : IEducationSpdEventPublisher, IDisposable
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
        string? traceParent,
        string? traceState,
        CancellationToken ct = default)
    {
        var headers = new Headers();
        AddHeader(headers, "traceparent", traceParent);
        AddHeader(headers, "tracestate", traceState);
        var result = await producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = messageKey,
            Value = messageJson,
            Headers = headers
        }, ct);
        logger.LogInformation(
            "Событие EducationSPD опубликовано в Kafka; ключ {MessageKey}, топик {Topic}, партиция {Partition}, смещение {Offset}",
            messageKey,
            result.Topic,
            result.Partition.Value,
            result.Offset.Value);
    }

    private static void AddHeader(Headers headers, string name, string? value)
    {
        if (!String.IsNullOrWhiteSpace(value))
        {
            headers.Add(name, Encoding.UTF8.GetBytes(value));
        }
    }

    public void Dispose() => producer.Dispose();
}
