using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TemplateApi.Data.Core.Configurations;

namespace TemplateApi.Data.Core.Migrations.PostgreSql;

internal sealed class PostgreSqlDbContext : TemplateDbContext
{
    public PostgreSqlDbContext(IOptions<ConnectionOptions> connectionOptions) : base(connectionOptions) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (Options.ConnectionString is null)
        {
            throw new InvalidOperationException("Не задана строка подключения к базе данных");
        }

        if (Options.DbProvider is not DbProvider.PostgreSql)
        {
            throw new InvalidOperationException("Ожидается провайдер БД PostgreSQL");
        }

        optionsBuilder.UseNpgsql(Options.ConnectionString);
    }
}
