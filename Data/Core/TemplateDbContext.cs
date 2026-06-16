using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TemplateApi.Data.Core.Configurations;

namespace TemplateApi.Data.Core;

/// <summary>
/// Шаблон контекста базы данных
/// </summary>
public class TemplateDbContext : DbContext
{
    /// <summary>
    /// Настройки подключения (используются производными контекстами миграций)
    /// </summary>
    protected readonly ConnectionOptions Options;

    /// <summary>
    /// Коллекция шаблонных объектов
    /// </summary>
    public DbSet<TemplateObject> TemplateObjects { get; set; }

    /// <summary>
    /// Конструктор контекста
    /// </summary>
    public TemplateDbContext(IOptions<ConnectionOptions> options)
    {
        Options = options.Value;
    }

    /// <summary>
    /// Конфигурация сущностей
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IDataMarkeredInterface).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Конфигурация провайдера — только для design-time (миграции).
    /// В рантайме контекст настраивается через AddDbContext в DI.
    /// </summary>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
        {
            return;
        }

        if (Options.ConnectionString is null)
        {
            throw new InvalidOperationException("Не задана строка подключения к базе данных");
        }

        switch (Options.DbProvider)
        {
            case DbProvider.Sqlite:
                optionsBuilder.UseSqlite(Options.ConnectionString);
                break;
            case DbProvider.PostgreSql:
                optionsBuilder.UseNpgsql(Options.ConnectionString);
                break;
            default:
                throw new InvalidOperationException("Неизвестный тип провайдера базы данных");
        }
    }
}
