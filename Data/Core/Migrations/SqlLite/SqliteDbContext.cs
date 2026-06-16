using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TemplateApi.Data.Core.Configurations;

namespace TemplateApi.Data.Core.Migrations.SqlLite;

internal sealed class SqliteDbContext : TemplateDbContext
{
    public SqliteDbContext(IOptions<ConnectionOptions> options) : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (Options.ConnectionString is null)
        {
            throw new InvalidOperationException("Не задана строка подключения к базе данных");
        }

        if (Options.DbProvider is not DbProvider.Sqlite)
        {
            throw new InvalidOperationException("Ожидается провайдер БД Sqlite");
        }

        optionsBuilder.UseSqlite(Options.ConnectionString);
    }
}
