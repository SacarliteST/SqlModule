using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using SQLModule.Client;
using SQLModule.Client.TargetDb;
using SQLModule.Data.Core.Configurations;
using SQLModule.Host;
using Testcontainers.PostgreSql;

namespace SQLModule.IntegrationTests.infrastructure;

public sealed class TestApplication :
    WebApplicationFactory<IHostMarker>,
    IAsyncLifetime
{
    private const string TestDbName = "storage_test";
    private const string TestUser = "postgresTestUser";
    private const string TestPassword = "postgresTestPassword";

    public ITargetDbClient TargetDbClient { get; private set; } = null!;

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
        var sp = BuildClientServiceProvider();
        TargetDbClient = sp.GetRequiredService<ITargetDbClient>();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var configurationValues = new Dictionary<string, string?>
        {
            { ConfigConstants.DbConnection, postgreContainer.GetConnectionString() },
            { ConfigConstants.DbProvider, DbProvider.PostgreSql.ToString() }
        };

        builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(configurationValues));
        base.ConfigureWebHost(builder);
    }

    private IServiceProvider BuildClientServiceProvider()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { $"{SqlModuleClientOptions.SectionKey}:BaseAddress", ClientOptions.BaseAddress.ToString() }
            })
            .Build();

        var services = new ServiceCollection();
        services.TryAddSingleton(Server);
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IHttpMessageHandlerBuilderFilter, TestServerMessageFilter>());
        services.AddSqlModuleClient(config);

        return services.BuildServiceProvider();
    }

    Task IAsyncLifetime.DisposeAsync() => postgreContainer.StopAsync();
}
