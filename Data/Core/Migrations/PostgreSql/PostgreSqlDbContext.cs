using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SQLModule.Data.Core.Configurations;

namespace SQLModule.Data.Core.Migrations.PostgreSql;

internal sealed class PostgreSqlDbContext : AppDbContext
{
    public PostgreSqlDbContext(IOptions<ConnectionOptions> connectionOptions) : base(connectionOptions) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (Options.ConnectionString is null)
        {
            throw new InvalidOperationException("Не задана строка подключения к базе данных");
        }

        optionsBuilder.UseNpgsql(Options.ConnectionString);
    }
}
