using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Http;
using Shouldly;

namespace TemplateApi.IntegrationsTest.infrastructure;

internal class TestServerMessageFilter : IHttpMessageHandlerBuilderFilter
{
    private readonly TestServer server;

    public TestServerMessageFilter(TestServer server)
    {
        this.server = server;
    }

    public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next)
    {
        return builder =>
        {
            next(builder);

            var serverHandler = new TestServerHandler(server);
            builder.AdditionalHandlers.Add(serverHandler);
        };
    }

    private sealed class TestServerHandler : DelegatingHandler
    {
        private readonly string serverAuthority;
        private readonly HttpMessageHandlerInvoker serverHandler;

        public TestServerHandler(TestServer server)
        {
            serverAuthority = GetFullAuthority(server.BaseAddress).ShouldNotBeNull();
            var testServerHandler = server.CreateHandler();
            serverHandler = new HttpMessageHandlerInvoker(testServerHandler);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (IsRequestToTestServer(request))
            {
                return serverHandler.ExecuteAsync(request, cancellationToken);
            }

            return base.SendAsync(request, cancellationToken);
        }

        private bool IsRequestToTestServer(HttpRequestMessage request)
        {
            return GetFullAuthority(request.RequestUri) == serverAuthority;
        }

        private static string? GetFullAuthority(Uri? uri) => uri?.GetLeftPart(UriPartial.Authority);
    }

    private sealed class HttpMessageHandlerInvoker : DelegatingHandler
    {
        public HttpMessageHandlerInvoker(HttpMessageHandler messageHandler)
        {
            InnerHandler = messageHandler;
        }

        public Task<HttpResponseMessage> ExecuteAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            SendAsync(request, cancellationToken);
    }
}
