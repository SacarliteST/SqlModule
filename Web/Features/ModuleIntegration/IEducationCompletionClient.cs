namespace SQLModule.Web.Features.ModuleIntegration;

internal interface IEducationCompletionClient
{
    Task<EducationCompletionDeliveryResult> CompleteAsync(
        Guid sessionId,
        string requestJson,
        CancellationToken ct);
}
