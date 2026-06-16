using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace TemplateApi.Client;

internal sealed class ErrorDelegatingHandler : DelegatingHandler
{
    /// <summary>
    /// Осуществляет проверку запроса на ошибку
    /// </summary>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode || response.StatusCode is HttpStatusCode.NotFound)
        {
            return response;
        }

        var problemInstance = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken)
                              ?? throw new InvalidOperationException("Получен неизвестный формат ответа");
        throw new WebException(problemInstance.Detail);
    }
}
