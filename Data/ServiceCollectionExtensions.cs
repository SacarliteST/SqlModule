using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SQLModule.Data.Core;
using SQLModule.Data.Core.Configurations;
using SQLModule.Data.Core.Migrations;

namespace SQLModule.Data;

/// <summary>
/// Добавляет методы расширения для регистрации сущностей слоя Data
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрация инфраструктуры данных
    /// </summary>
    public static IServiceCollection AddData(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ConnectionOptions>()
            .BindConfiguration(ConnectionOptions.OptionsKey);

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<AuditInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var connOpts = sp.GetRequiredService<IOptions<ConnectionOptions>>().Value;

            if (connOpts.ConnectionString is null)
            {
                throw new InvalidOperationException("Не задана строка подключения к базе данных");
            }

            switch (connOpts.DbProvider)
            {
                case DbProvider.Sqlite:
                    options.UseSqlite(connOpts.ConnectionString);
                    break;
                case DbProvider.PostgreSql:
                    options.UseNpgsql(connOpts.ConnectionString);
                    break;
                default:
                    throw new InvalidOperationException("Неизвестный тип провайдера базы данных");
            }

            options.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());

#if DEBUG
            options.LogTo(Console.WriteLine, LogLevel.Information);
#endif
        });

        services.AddScoped<IMigrationManager, DatabaseMigrationManager>();

        return services;
    }
}
