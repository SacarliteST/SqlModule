using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using SQLModule.Client;
using SQLModule.Client.SchemaBuilder;
using SQLModule.Data.Core.Configurations;
using SQLModule.Host;
using SQLModule.Host.Features.DbmsCatalog.DbmsDictionary.Sandbox;
using Testcontainers.PostgreSql;

namespace SQLModule.IntegrationTests.infrastructure;

/// <summary>
/// Фабрика тест-приложения для Docker-тестов: использует реальный ISandboxExecutor.
/// </summary>
public sealed class DockerTestApplication :
    WebApplicationFactory<IHostMarker>,
    IAsyncLifetime
{
    private const string TestDbName = "storage_docker_test";
    private const string TestUser = "postgresTestUser";
    private const string TestPassword = "postgresTestPassword";

    /// <summary>Типизированный клиент для построителя схемы.</summary>
    public ISchemaBuilderClient SchemaBuilderClient { get; private set; } = null!;

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
        SchemaBuilderClient = sp.GetRequiredService<ISchemaBuilderClient>();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var configurationValues = new Dictionary<string, string?>
        {
            { ConfigConstants.DbConnection, postgreContainer.GetConnectionString() },
            { ConfigConstants.DbProvider, DbProvider.PostgreSql.ToString() }
        };

        builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(configurationValues));
        builder.ConfigureServices(services =>
        {
            // Только IDbmsProbe подменяется, чтобы создание СУБД-словаря не запускало Docker.
            // ISandboxExecutor НЕ переопределяется — используется реальный TestcontainersSandboxExecutor.
            services.AddSingleton<IDbmsProbe, AlwaysOkProbe>();
        });
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
