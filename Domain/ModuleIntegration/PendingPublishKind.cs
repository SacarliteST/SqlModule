namespace SQLModule.Domain.ModuleIntegration;

/// <summary>Канал доставки записи integration outbox.</summary>
public enum PendingPublishKind
{
    Event,
    Grade
}
