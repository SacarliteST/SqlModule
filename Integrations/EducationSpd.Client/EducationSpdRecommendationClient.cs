using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EducationSPD.Contracts;
using EducationSPD.Contracts.Recommendations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SQLModule.EducationSpd.Abstractions;

namespace SQLModule.EducationSpd.Client;

internal sealed class EducationSpdRecommendationClient(
    HttpClient httpClient,
    IOptions<EducationSpdRecommendationClientOptions> options,
    ILogger<EducationSpdRecommendationClient> logger) : IEducationSpdRecommendationClient
{
    public async Task<RecommendationClientResult> RankAsync(
        RankRecommendationsRequest request,
        CancellationToken ct = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.Value.RequestTimeoutSeconds));
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            try
            {
                using var message = CreateRequest(request);
                using var response = await httpClient.SendAsync(
                    message,
                    HttpCompletionOption.ResponseHeadersRead,
                    timeout.Token);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<RankRecommendationsResponse>(
                        cancellationToken: timeout.Token);
                    return IsValidResponse(request, result)
                        ? RecommendationClientResult.Success(result!)
                        : RecommendationClientResult.Unavailable("InvalidResponse");
                }

                if (!IsTransient(response.StatusCode) || attempt == 2)
                {
                    logger.LogWarning(
                        "EducationSPD не выполнил ранжирование; HTTP-статус {StatusCode}",
                        (int)response.StatusCode);
                    return RecommendationClientResult.Unavailable($"Http{(int)response.StatusCode}");
                }
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                return RecommendationClientResult.Unavailable("Timeout");
            }
            catch (HttpRequestException exception) when (attempt == 1)
            {
                logger.LogWarning(
                    "Временная сетевая ошибка ранжирования EducationSPD; тип {ExceptionType}",
                    exception.GetType().Name);
            }
            catch (HttpRequestException exception)
            {
                logger.LogWarning(
                    "EducationSPD недоступен для ранжирования; тип {ExceptionType}",
                    exception.GetType().Name);
                return RecommendationClientResult.Unavailable("NetworkError");
            }
            catch (JsonException exception)
            {
                logger.LogWarning(
                    "EducationSPD вернул некорректный ответ ранжирования; тип {ExceptionType}",
                    exception.GetType().Name);
                return RecommendationClientResult.Unavailable("InvalidResponse");
            }
        }

        return RecommendationClientResult.Unavailable("Unavailable");
    }

    private HttpRequestMessage CreateRequest(RankRecommendationsRequest request)
    {
        var baseUri = new Uri(options.Value.BaseUrl, UriKind.Absolute);
        var message = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri(baseUri, ApiRoutes.Recommendations.Rank));
        message.Headers.Add("X-Service-Key", options.Value.ServiceKey);
        message.Content = JsonContent.Create(request);
        return message;
    }

    private static bool IsTransient(HttpStatusCode statusCode) => (int)statusCode >= 500;

    private static bool IsValidResponse(
        RankRecommendationsRequest request,
        RankRecommendationsResponse? response)
    {
        if (response is null || response.ModelVersion <= 0 || request.Candidates is null ||
            response.Items.Count != request.Candidates.Count)
        {
            return false;
        }

        var expected = request.Candidates.Select(candidate => candidate.TaskId).ToHashSet();
        var actual = response.Items.Select(candidate => (Guid?)candidate.TaskId).ToArray();
        return actual.Distinct().Count() == actual.Length && actual.All(expected.Contains);
    }
}
