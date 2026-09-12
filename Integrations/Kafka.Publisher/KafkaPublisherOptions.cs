namespace SQLModule.Kafka.Publisher;

internal sealed class KafkaPublisherOptions
{
    public string BootstrapServers { get; init; } = String.Empty;
    public string EventsTopic { get; init; } = String.Empty;
}
