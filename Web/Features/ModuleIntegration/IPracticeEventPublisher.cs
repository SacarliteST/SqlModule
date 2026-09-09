namespace SQLModule.Web.Features.ModuleIntegration;

internal interface IPracticeEventPublisher
{
    Task PublishAsync(Guid sessionId, string messageJson, CancellationToken ct);
}
