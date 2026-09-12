using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using SQLModule.PlatformIntegration.Abstractions;
using SQLModule.PlatformIntegration.Contracts;

namespace SQLModule.Education.Client;

internal sealed class EducationCompletionClient(
    HttpClient httpClient,
    ILogger<EducationCompletionClient> logger) : IEducationCompletionClient
{
    public async Task<EducationCompletionDeliveryResult> CompleteAsync(
        Guid sessionId,
        PracticeCompletionRequest request,
        CancellationToken ct = default)
    {
        logger.LogInformation(
            "Отправляется завершение платформенной сессии {SessionId} в Education",
            sessionId);
        using var response = await httpClient.PostAsJsonAsync(
            ApiRoutes.ModuleSessions.ForComplete(sessionId),
            request,
            PlatformIntegrationJson.Default,
            ct);
        if (response.IsSuccessStatusCode)
        {
            logger.LogInformation(
                "Education принял завершение платформенной сессии {SessionId}; статус {StatusCode}",
                sessionId,
                (int)response.StatusCode);
            return EducationCompletionDeliveryResult.Accepted;
        }

        logger.LogWarning(
            "Education отклонил завершение платформенной сессии {SessionId}; статус {StatusCode}",
            sessionId,
            (int)response.StatusCode);
        return response.StatusCode switch
        {
            HttpStatusCode.Conflict => EducationCompletionDeliveryResult.TerminalConflict,
            HttpStatusCode.Unauthorized => EducationCompletionDeliveryResult.AuthenticationRejected,
            >= HttpStatusCode.BadRequest and < HttpStatusCode.InternalServerError =>
                EducationCompletionDeliveryResult.NonRetryableRejection,
            _ => throw new HttpRequestException(
                "Education временно не принял завершение платформенной сессии.",
                null,
                response.StatusCode),
        };
    }
}
