using Microsoft.Extensions.Http;

namespace SQLModule.IntegrationTests.infrastructure;

/// <summary>
/// Фильтр HttpClient-пайплайна: добавляет заголовки X-Test-UserId / X-Test-Roles
/// из <see cref="TestUserContext"/> в каждый исходящий запрос типизированного клиента.
/// Должен регистрироваться ПОСЛЕ <see cref="TestServerMessageFilter"/>, чтобы стать
/// внешним обработчиком и добавить заголовки до передачи запроса серверу.
/// </summary>
internal sealed class TestAuthMessageFilter : IHttpMessageHandlerBuilderFilter
{
    private readonly TestUserContext userContext;

    public TestAuthMessageFilter(TestUserContext userContext)
    {
        this.userContext = userContext;
    }

    public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next)
    {
        return builder =>
        {
            next(builder);
            builder.AdditionalHandlers.Add(new TestAuthDelegatingHandler(userContext));
        };
    }
}

internal sealed class TestAuthDelegatingHandler : DelegatingHandler
{
    private readonly TestUserContext userContext;

    public TestAuthDelegatingHandler(TestUserContext userContext)
    {
        this.userContext = userContext;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        request.Headers.Remove("X-Test-UserId");
        request.Headers.Remove("X-Test-Roles");
        request.Headers.Remove("X-Test-DisplayName");
        request.Headers.Add("X-Test-UserId", userContext.UserId);
        request.Headers.Add("X-Test-Roles", userContext.Roles);
        request.Headers.Add("X-Test-DisplayName", userContext.DisplayName);
        return base.SendAsync(request, ct);
    }
}
