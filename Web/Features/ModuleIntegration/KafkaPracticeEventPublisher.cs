using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace SQLModule.Web.Features.ModuleIntegration;

internal sealed class KafkaPracticeEventPublisher : IPracticeEventPublisher, IDisposable
{
    private readonly IProducer<string, string> producer;
    private readonly string topic;

    public KafkaPracticeEventPublisher(IOptions<ModuleIntegrationOptions> options)
    {
        var kafka = options.Value.Kafka;
        topic = kafka.EventsTopic;
        producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = kafka.BootstrapServers,
            EnableIdempotence = true,
            Acks = Acks.All
        }).Build();
    }

    public async Task PublishAsync(Guid sessionId, string messageJson, CancellationToken ct)
    {
        await producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = sessionId.ToString("D"),
            Value = messageJson
        }, ct);
    }

    public void Dispose() => producer.Dispose();
}
