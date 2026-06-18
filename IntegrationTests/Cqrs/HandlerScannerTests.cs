using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Host;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;
namespace SQLModule.IntegrationTests.Cqrs;

public sealed class HandlerScannerTests
{
    [Fact(DisplayName = "Все IRequest<> из Host.Features имеют зарегистрированный хендлер")]
    public void AllRequestHandlers_AreRegistered()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCqrs();
        services.AddFeatures();

        var iRequestOpenType = typeof(IRequest<>);
        var hostAssembly = typeof(IHostMarker).Assembly;

        var requestInfos = hostAssembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == iRequestOpenType)
                .Select(i => (RequestType: t, ResponseType: i.GetGenericArguments()[0])))
            .ToList();

        var unregistered = requestInfos
            .Where(info =>
            {
                var handlerType = typeof(IRequestHandler<,>)
                    .MakeGenericType(info.RequestType, info.ResponseType);
                return !services.Any(sd => sd.ServiceType == handlerType);
            })
            .Select(info =>
                $"IRequestHandler<{info.RequestType.Name}, {info.ResponseType.Name}>")
            .ToList();

        unregistered.ShouldBeEmpty(
            $"Незарегистрированные хендлеры:{Environment.NewLine}" +
            string.Join(Environment.NewLine, unregistered));
    }
}
