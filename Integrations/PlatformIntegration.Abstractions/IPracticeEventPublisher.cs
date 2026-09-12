using SQLModule.PlatformIntegration.Contracts;

namespace SQLModule.PlatformIntegration.Abstractions;

public interface IPracticeEventPublisher
{
    Task PublishAsync(PracticeEventMessage message, CancellationToken ct = default);
}
