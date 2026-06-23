using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SQLModule.Data.Core.Configurations;
using SQLModule.Data.Core.Migrations.PostgreSql;

namespace SQLModule.Data.Core.Migrations;

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

        logger.LogInformation("Применение миграций для PostgreSql");
        await new PostgreSqlDbContext(options).Database.MigrateAsync();

        logger.LogInformation("Миграции применены. Затраченное время: {Elapsed:0.0000}мс", timer.Elapsed.Milliseconds);
    }
}
