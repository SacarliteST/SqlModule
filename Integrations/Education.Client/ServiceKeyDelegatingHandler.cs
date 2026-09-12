using Microsoft.Extensions.Options;

namespace SQLModule.Education.Client;

internal sealed class ServiceKeyDelegatingHandler(
    IOptions<EducationClientOptions> options) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.Headers.Add("X-Service-Key", options.Value.ServiceKey);
        return base.SendAsync(request, cancellationToken);
    }
}
