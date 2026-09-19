using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SQLModule.Domain.Common;
using SQLModule.Domain.Exceptions;
using SQLModule.Domain.Schema;
using SQLModule.Domain.Training;

namespace SQLModule.Data.Core;

/// <summary>
/// Не даёт менять схему и данные учебной базы, пока её использует опубликованное задание: результат эталона и
/// схема, которую видит студент, не должны молча расходиться с опубликованной версией оценки.
/// Изменить базу можно после архивации заданий.
/// </summary>
internal sealed class TargetDbPublishedUsageInterceptor : SaveChangesInterceptor
{
    internal const string ErrorCode = "TargetDb.PublishedTaskReferenceExists";
    private const int MaxTaskIdsInResponse = 20;

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is { } context)
        {
            EnsureAsync(context, CancellationToken.None).GetAwaiter().GetResult();
        }

        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is { } context)
        {
            await EnsureAsync(context, cancellationToken);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static async Task EnsureAsync(DbContext context, CancellationToken ct)
    {
        if (TargetDbEditBypass.IsActive)
        {
            return;
        }

        var targetDbIds = new HashSet<Guid>();
        var tableIds = new HashSet<Guid>();
        var attributeIds = new HashSet<Guid>();
        var recordIds = new HashSet<Guid>();
        foreach (var entry in context.ChangeTracker.Entries()
                     .Where(value => value.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            switch (entry.Entity)
            {
                case MetaTable table:
                    targetDbIds.Add(table.TargetDbId);
                    break;
                case TargetDb targetDb when entry.State == EntityState.Deleted ||
                                            entry.State == EntityState.Modified &&
                                            entry.Property(nameof(TargetDb.SchemaVersion)).IsModified:
                    targetDbIds.Add(targetDb.Id);
                    break;
                case MetaAttribute attribute:
                    tableIds.Add(attribute.MetaTableId);
                    break;
                case DataRecord record:
                    tableIds.Add(record.MetaTableId);
                    break;
                case MetaRelationship relationship:
                    attributeIds.Add(relationship.SourceAttributeId);
                    attributeIds.Add(relationship.TargetAttributeId);
                    break;
                case AttributeParameterValue value:
                    attributeIds.Add(value.MetaAttributeId);
                    break;
                case CellValue cell:
                    recordIds.Add(cell.DataRecordId);
                    attributeIds.Add(cell.MetaAttributeId);
                    break;
            }
        }

        if (targetDbIds.Count == 0 && tableIds.Count == 0 && attributeIds.Count == 0 && recordIds.Count == 0)
        {
            return;
        }

        if (attributeIds.Count > 0)
        {
            tableIds.UnionWith(await context.Set<MetaAttribute>().AsNoTracking()
                .Where(value => attributeIds.Contains(value.Id))
                .Select(value => value.MetaTableId)
                .ToListAsync(ct));
        }

        if (recordIds.Count > 0)
        {
            tableIds.UnionWith(await context.Set<DataRecord>().AsNoTracking()
                .Where(value => recordIds.Contains(value.Id))
                .Select(value => value.MetaTableId)
                .ToListAsync(ct));
        }

        if (tableIds.Count > 0)
        {
            targetDbIds.UnionWith(await context.Set<MetaTable>().AsNoTracking()
                .Where(value => tableIds.Contains(value.Id))
                .Select(value => value.TargetDbId)
                .ToListAsync(ct));
        }

        if (targetDbIds.Count == 0)
        {
            return;
        }

        var taskIds = await context.Set<SqlTask>().AsNoTracking()
            .Where(task => task.PublicationStatus == PublicationStatus.Published &&
                           targetDbIds.Contains(task.SqlQuery.TargetDbId))
            .OrderBy(task => task.Id)
            .Select(task => task.Id)
            .ToListAsync(ct);
        if (taskIds.Count == 0)
        {
            return;
        }

        throw new DomainConflictException(
            ErrorCode,
            $"Учебная база используется опубликованными заданиями ({taskIds.Count}). " +
            "Чтобы изменить схему или данные, сначала архивируйте эти задания.",
            new Dictionary<string, string[]>
            {
                ["taskIds"] = taskIds.Take(MaxTaskIdsInResponse).Select(id => id.ToString("D")).ToArray()
            });
    }
}
