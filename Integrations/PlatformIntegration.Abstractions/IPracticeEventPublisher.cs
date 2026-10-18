using SQLModule.PlatformIntegration.Contracts;

namespace SQLModule.PlatformIntegration.Abstractions;

public interface IPracticeEventPublisher
{
    Task PublishAsync(Guid sessionId, string messageJson, CancellationToken ct = default);
}
