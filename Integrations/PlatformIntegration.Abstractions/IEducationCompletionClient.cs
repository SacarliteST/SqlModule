using SQLModule.PlatformIntegration.Contracts;

namespace SQLModule.PlatformIntegration.Abstractions;

public interface IEducationCompletionClient
{
    Task<EducationCompletionDeliveryResult> CompleteAsync(
        Guid sessionId,
        PracticeCompletionRequest request,
        CancellationToken ct = default);
}
