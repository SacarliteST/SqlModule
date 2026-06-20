using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using SQLModule.Client;
using SQLModule.Client.Attempt;
using SQLModule.Client.DataRecord;
using SQLModule.Client.MetaAttribute;
using SQLModule.Client.MetaRelationship;
using SQLModule.Client.MetaTable;
using SQLModule.Client.SqlQuery;
using SQLModule.Client.SqlTask;
using SQLModule.Client.TargetDb;
using SQLModule.Client.Topic;
using SQLModule.Data.Core.Configurations;
using SQLModule.Host;
using Testcontainers.PostgreSql;

namespace SQLModule.IntegrationTests.infrastructure;

/// <summary>
/// Фабрика тест-приложения: поднимает in-process ASP.NET Core хост с PostgreSQL-контейнером
/// и предоставляет готовые типизированные клиенты для интеграционных тестов.
/// </summary>
public sealed class TestApplication :
    WebApplicationFactory<IHostMarker>,
    IAsyncLifetime
{
    private const string TestDbName = "storage_test";
    private const string TestUser = "postgresTestUser";
    private const string TestPassword = "postgresTestPassword";

    /// <summary>Типизированный клиент для работы с целевыми БД.</summary>
    public ITargetDbClient TargetDbClient { get; private set; } = null!;

    /// <summary>Типизированный клиент для работы с темами тренажёра.</summary>
    public ITopicClient TopicClient { get; private set; } = null!;

    /// <summary>Типизированный клиент для работы с SQL-заданиями тренажёра.</summary>
    public ISqlTaskClient SqlTaskClient { get; private set; } = null!;

    /// <summary>Типизированный клиент для работы с эталонными SQL-запросами.</summary>
    public ISqlQueryClient SqlQueryClient { get; private set; } = null!;

    /// <summary>Типизированный клиент для работы с попытками выполнения заданий.</summary>
    public IAttemptClient AttemptClient { get; private set; } = null!;

    /// <summary>Типизированный клиент для работы с мета-таблицами.</summary>
    public IMetaTableClient MetaTableClient { get; private set; } = null!;

    /// <summary>Типизированный клиент для работы с FK-связями между мета-атрибутами.</summary>
    public IMetaRelationshipClient MetaRelationshipClient { get; private set; } = null!;

    /// <summary>Типизированный клиент для работы с мета-атрибутами (колонками).</summary>
    public IMetaAttributeClient MetaAttributeClient { get; private set; } = null!;

    /// <summary>Типизированный клиент для работы со строками данных (EAV-якоря).</summary>
    public IDataRecordClient DataRecordClient { get; private set; } = null!;

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
        TopicClient = sp.GetRequiredService<ITopicClient>();
        SqlTaskClient = sp.GetRequiredService<ISqlTaskClient>();
        SqlQueryClient = sp.GetRequiredService<ISqlQueryClient>();
        AttemptClient = sp.GetRequiredService<IAttemptClient>();
        MetaTableClient = sp.GetRequiredService<IMetaTableClient>();
        MetaRelationshipClient = sp.GetRequiredService<IMetaRelationshipClient>();
        MetaAttributeClient = sp.GetRequiredService<IMetaAttributeClient>();
        DataRecordClient = sp.GetRequiredService<IDataRecordClient>();
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

    /// <summary>
    /// Строит изолированный DI-контейнер для клиентов: регистрирует <see cref="TestServerMessageFilter"/>,
    /// чтобы HTTP-запросы клиентов шли через in-process тест-сервер, а не в реальную сеть.
    /// </summary>
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
