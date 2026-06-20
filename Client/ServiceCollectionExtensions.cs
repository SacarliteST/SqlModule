using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SQLModule.Client.Attempt;
using SQLModule.Client.MetaRelationship;
using SQLModule.Client.MetaTable;
using SQLModule.Client.SqlQuery;
using SQLModule.Client.SqlTask;
using SQLModule.Client.TargetDb;
using SQLModule.Client.Topic;

namespace SQLModule.Client;

/// <summary>Методы регистрации типизированных HTTP-клиентов в DI.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует типизированных клиентов модуля и базовую HTTP-инфраструктуру.
    /// <see cref="SqlModuleClientOptions"/> привязываются из секции
    /// <see cref="SqlModuleClientOptions.SectionKey"/> переданной конфигурации.
    /// </summary>
    public static IServiceCollection AddSqlModuleClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IConfiguration>(configuration);

        services.AddOptions<SqlModuleClientOptions>()
            .BindConfiguration(SqlModuleClientOptions.SectionKey);

        services.AddTransient<ErrorDelegatingHandler>();

        services.AddHttpClient<ITargetDbClient, TargetDbClient>((sp, http) =>
                http.BaseAddress = sp.GetRequiredService<IOptions<SqlModuleClientOptions>>()
                    .Value.BaseAddress)
            .AddHttpMessageHandler<ErrorDelegatingHandler>();

        services.AddHttpClient<ITopicClient, TopicClient>((sp, http) =>
                http.BaseAddress = sp.GetRequiredService<IOptions<SqlModuleClientOptions>>()
                    .Value.BaseAddress)
            .AddHttpMessageHandler<ErrorDelegatingHandler>();

        services.AddHttpClient<ISqlTaskClient, SqlTaskClient>((sp, http) =>
                http.BaseAddress = sp.GetRequiredService<IOptions<SqlModuleClientOptions>>()
                    .Value.BaseAddress)
            .AddHttpMessageHandler<ErrorDelegatingHandler>();

        services.AddHttpClient<ISqlQueryClient, SqlQueryClient>((sp, http) =>
                http.BaseAddress = sp.GetRequiredService<IOptions<SqlModuleClientOptions>>()
                    .Value.BaseAddress)
            .AddHttpMessageHandler<ErrorDelegatingHandler>();

        services.AddHttpClient<IAttemptClient, AttemptClient>((sp, http) =>
                http.BaseAddress = sp.GetRequiredService<IOptions<SqlModuleClientOptions>>()
                    .Value.BaseAddress)
            .AddHttpMessageHandler<ErrorDelegatingHandler>();

        services.AddHttpClient<IMetaTableClient, MetaTableClient>((sp, http) =>
                http.BaseAddress = sp.GetRequiredService<IOptions<SqlModuleClientOptions>>()
                    .Value.BaseAddress)
            .AddHttpMessageHandler<ErrorDelegatingHandler>();

        services.AddHttpClient<IMetaRelationshipClient, MetaRelationshipClient>((sp, http) =>
                http.BaseAddress = sp.GetRequiredService<IOptions<SqlModuleClientOptions>>()
                    .Value.BaseAddress)
            .AddHttpMessageHandler<ErrorDelegatingHandler>();

        return services;
    }
}
