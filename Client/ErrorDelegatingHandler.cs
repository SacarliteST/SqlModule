using System.Net;
using System.Net.Http.Json;

namespace SQLModule.Client;

/// <summary>
/// HTTP-обработчик, преобразующий ошибочные ответы API в типизированные исключения.
/// Пропускает насквозь 2xx и 404 — решение о 404 принимает вызывающий метод.
/// </summary>
internal sealed class ErrorDelegatingHandler : DelegatingHandler
{
    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound)
        {
            return response;
        }

        ApiProblem? problem = null;
        try
        {
            problem = await response.Content
                .ReadFromJsonAsync<ApiProblem>(ClientJson.Options, cancellationToken);
        }
        catch (Exception)
        {
            // тело не распарсилось — problem остаётся null
        }

        throw (int)response.StatusCode switch
        {
            400 or 422 => new ValidationException((int)response.StatusCode, problem),
            409 => new ConflictException((int)response.StatusCode, problem),
            _ => new ApiException((int)response.StatusCode, problem)
        };
    }
}
