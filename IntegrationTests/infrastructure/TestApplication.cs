using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
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

    Task IAsyncLifetime.DisposeAsync()
    {
        return postgreContainer.StopAsync();
    }
}
