using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;

namespace SQLModule.Web.Features.ModuleIntegration;

internal sealed class EducationCompletionClient(
    HttpClient httpClient,
    IOptions<ModuleIntegrationOptions> options) : IEducationCompletionClient
{
    public async Task<EducationCompletionDeliveryResult> CompleteAsync(
        Guid sessionId,
        string requestJson,
        CancellationToken ct)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"api/v1/module-sessions/{sessionId:D}/complete");
        request.Headers.Add("X-Service-Key", options.Value.ServiceKey);
        request.Content = new StringContent(requestJson, Encoding.UTF8);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (response.IsSuccessStatusCode)
        {
            return EducationCompletionDeliveryResult.Accepted;
        }

        return response.StatusCode switch
        {
            HttpStatusCode.Conflict => EducationCompletionDeliveryResult.TerminalConflict,
            HttpStatusCode.Unauthorized => EducationCompletionDeliveryResult.AuthenticationRejected,
            >= HttpStatusCode.BadRequest and < HttpStatusCode.InternalServerError =>
                EducationCompletionDeliveryResult.NonRetryableRejection,
            _ => throw new HttpRequestException(
                "Education временно не принял завершение платформенной сессии.",
                null,
                response.StatusCode)
        };
    }
}
