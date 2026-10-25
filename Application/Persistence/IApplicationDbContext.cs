using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SQLModule.Domain.DbmsCatalog;
using SQLModule.Domain.ModuleIntegration;
using SQLModule.Domain.Schema;
using SQLModule.Domain.Training;

namespace SQLModule.Application.Persistence;

/// <summary>
/// Порт доступа к данным для сценариев Application. Реализуется контекстом EF Core в проекте Data;
/// обобщённых репозиториев нет — сценарии работают с наборами сущностей напрямую.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<DbmsDictionary> DbmsDictionaries { get; }

    DbSet<PhysicalType> PhysicalTypes { get; }

    DbSet<ParameterDefinition> ParameterDefinitions { get; }

    DbSet<TargetDb> TargetDbs { get; }

    DbSet<MetaTable> MetaTables { get; }

    DbSet<MetaAttribute> MetaAttributes { get; }

    DbSet<MetaRelationship> MetaRelationships { get; }

    DbSet<AttributeParameterValue> AttributeParameterValues { get; }

    DbSet<DataRecord> DataRecords { get; }

    DbSet<CellValue> CellValues { get; }

    DbSet<MutationReceipt> MutationReceipts { get; }

    DbSet<Topic> Topics { get; }

    DbSet<SqlQuery> SqlQueries { get; }

    DbSet<SqlTask> SqlTasks { get; }

    DbSet<Attempt> Attempts { get; }

    DbSet<TaskValidationConfiguration> TaskValidationConfigurations { get; }

    DbSet<ValidationCheck> ValidationChecks { get; }

    DbSet<TaskValidationVersion> TaskValidationVersions { get; }

    DbSet<AttemptCheckResult> AttemptCheckResults { get; }

    DbSet<ModuleSession> ModuleSessions { get; }

    DbSet<PendingPublish> PendingPublishes { get; }

    /// <summary>Доступ к транзакциям и подключению.</summary>
    DatabaseFacade Database { get; }

    /// <summary>Доступ к состоянию отслеживаемой сущности.</summary>
    EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;

    /// <summary>Добавляет сущность в контекст.</summary>
    EntityEntry<TEntity> Add<TEntity>(TEntity entity) where TEntity : class;

    /// <summary>Отслеживание изменений.</summary>
    ChangeTracker ChangeTracker { get; }

    /// <summary>Сохраняет изменения в базе данных.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
