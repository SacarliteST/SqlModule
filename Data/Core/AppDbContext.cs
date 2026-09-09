using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SQLModule.Data.Core.Configurations;
using SQLModule.Domain.DbmsCatalog;
using SQLModule.Domain.ModuleIntegration;
using SQLModule.Domain.Schema;
using SQLModule.Domain.Training;

namespace SQLModule.Data.Core;

/// <summary>Контекст базы данных приложения.</summary>
public class AppDbContext : DbContext
{
    /// <summary>Настройки подключения (используются производными контекстами миграций).</summary>
    protected readonly ConnectionOptions Options;

    /// <summary>Справочник СУБД</summary>
    public DbSet<DbmsDictionary> DbmsDictionaries { get; set; }

    /// <summary>Физические типы данных</summary>
    public DbSet<PhysicalType> PhysicalTypes { get; set; }

    /// <summary>Определения параметров физических типов</summary>
    public DbSet<ParameterDefinition> ParameterDefinitions { get; set; }

    /// <summary>Целевые базы данных (песочницы)</summary>
    public DbSet<TargetDb> TargetDbs { get; set; }

    /// <summary>Мета-таблицы</summary>
    public DbSet<MetaTable> MetaTables { get; set; }

    /// <summary>Мета-атрибуты (колонки)</summary>
    public DbSet<MetaAttribute> MetaAttributes { get; set; }

    /// <summary>Связи между мета-атрибутами</summary>
    public DbSet<MetaRelationship> MetaRelationships { get; set; }

    /// <summary>Значения параметров атрибутов</summary>
    public DbSet<AttributeParameterValue> AttributeParameterValues { get; set; }

    /// <summary>Строки данных (EAV)</summary>
    public DbSet<DataRecord> DataRecords { get; set; }

    /// <summary>Значения ячеек (EAV)</summary>
    public DbSet<CellValue> CellValues { get; set; }

    /// <summary>Сохранённые результаты идемпотентных mutation-запросов.</summary>
    public DbSet<MutationReceipt> MutationReceipts { get; set; }

    /// <summary>Темы заданий</summary>
    public DbSet<Topic> Topics { get; set; }

    /// <summary>Эталонные SQL-запросы</summary>
    public DbSet<SqlQuery> SqlQueries { get; set; }

    /// <summary>Задания тренажёра</summary>
    public DbSet<SqlTask> SqlTasks { get; set; }

    /// <summary>Попытки выполнения заданий</summary>
    public DbSet<Attempt> Attempts { get; set; }

    /// <summary>Контексты запусков SQL-модуля из основной платформы.</summary>
    public DbSet<ModuleSession> ModuleSessions { get; set; }

    /// <summary>Сообщения integration outbox, ожидающие доставки платформе.</summary>
    public DbSet<PendingPublish> PendingPublishes { get; set; }

    /// <param name="dbContextOptions">Настройки runtime-контекста из DI, включая interceptors.</param>
    /// <param name="options">Параметры подключения.</param>
    public AppDbContext(
        DbContextOptions<AppDbContext> dbContextOptions,
        IOptions<ConnectionOptions> options) : base(dbContextOptions)
    {
        Options = options.Value;
    }

    /// <summary>Конструктор design-time контекстов миграций.</summary>
    protected AppDbContext(IOptions<ConnectionOptions> options)
    {
        Options = options.Value;
    }

    /// <inheritdoc/>
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

        optionsBuilder.UseNpgsql(Options.ConnectionString);
    }
}
