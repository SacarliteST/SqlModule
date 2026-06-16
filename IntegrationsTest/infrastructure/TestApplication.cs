using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using TemplateApi.Client;
using TemplateApi.Client.Template;
using TemplateApi.Data.Core.Configurations;
using TemplateApi.Host;
using Testcontainers.PostgreSql;

namespace TemplateApi.IntegrationsTest.infrastructure;

public sealed class TestApplication :
    WebApplicationFactory<IHostMarker>,
    IAsyncLifetime
{
    private const string TestDbName = "storage_test";
    private const string TestUser = "postgresTestUser";
    private const string TestPassword = "postgresTestPassword";

    public required ITemplateClient TemplateClient;

    private IServiceProvider? serviceProvider;

    private readonly PostgreSqlContainer postgreContainer
        = new PostgreSqlBuilder()
                    .WithImage(DockerImages.PostgreSql)
                    .WithDatabase(TestDbName)
                    .WithUsername(TestUser)
                    .WithPassword(TestPassword)
                    .WithName($"{TestDbName}_PostgreSql_{Guid.NewGuid()}")
                    .WithCleanUp(true)
                    .Build();

    public async Task InitializeAsync()
    {
        await postgreContainer.StartAsync();

        serviceProvider = ConfigureClients();

        TemplateClient = serviceProvider.GetRequiredService<ITemplateClient>();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var configurationValues = new Dictionary<string, string?>
        {
            {ConfigConstants.DbConnection, postgreContainer.GetConnectionString()},
            {ConfigConstants.DbProvider, DbProvider.PostgreSql.ToString()}
        };

        builder.ConfigureAppConfiguration(config => { config.AddInMemoryCollection(configurationValues); });
        base.ConfigureWebHost(builder);
    }

    private IServiceProvider ConfigureClients()
    {
        var settings = new Dictionary<string, string?>
        {
            { ConfigConstants.ApiUrl, ClientOptions.BaseAddress.ToString() },
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.TryAddSingleton(Server);
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IHttpMessageHandlerBuilderFilter,
            TestServerMessageFilter>());
        services.AddClient();
        services.AddTransient<IConfiguration>(_ => config);

        return services.BuildServiceProvider();
    }

    Task IAsyncLifetime.DisposeAsync()
    {
        return postgreContainer.StopAsync();
    }
}
