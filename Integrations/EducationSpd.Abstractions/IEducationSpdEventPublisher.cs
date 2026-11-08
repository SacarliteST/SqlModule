namespace SQLModule.EducationSpd.Abstractions;

/// <summary>Публикует интеграционное событие SQL-модуля в Kafka EducationSPD.</summary>
public interface IEducationSpdEventPublisher
{
    Task PublishAsync(
        string topic,
        string messageKey,
        string messageJson,
        string? traceParent,
        string? traceState,
        CancellationToken ct = default);
}
