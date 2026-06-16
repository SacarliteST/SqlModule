using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TemplateApi.Data.Core.Configurations;
using TemplateApi.Data.Core.Migrations.PostgreSql;
using TemplateApi.Data.Core.Migrations.SqlLite;

namespace TemplateApi.Data.Core.Migrations;

internal sealed class DatabaseMigrationManager : IMigrationManager
{
    private readonly ILogger<DatabaseMigrationManager> logger;
    private readonly IOptions<ConnectionOptions> options;

    public DatabaseMigrationManager(ILogger<DatabaseMigrationManager> logger, IOptions<ConnectionOptions> options)
    {
        this.logger = logger;
        this.options = options;
    }

    public async Task MigrateAsync()
    {
        var timer = Stopwatch.StartNew();

        switch (options.Value.DbProvider)
        {
            case DbProvider.Sqlite:
                logger.LogInformation("Применение миграций для Sqlite");
                await new SqliteDbContext(options).Database.MigrateAsync();
                break;
            case DbProvider.PostgreSql:
                logger.LogInformation("Применение миграций для PostgreSql");
                await new PostgreSqlDbContext(options).Database.MigrateAsync();
                break;
            default:
                throw new InvalidOperationException("Неизвестный тип провайдера базы данных");
        }

        logger.LogInformation("Миграции применены. Затраченное время: {Elapsed:0.0000}мс", timer.Elapsed.Milliseconds);
    }
}
