using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Cqrs;
using SQLModule.Application.ModuleIntegration;
using SQLModule.Application.Persistence;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Student;
using SQLModule.Domain.Training;

namespace SQLModule.Application.Training.Student;

/// <summary>Возвращает безопасную физическую схему базы задания.</summary>
public sealed class GetStudentTaskSchemaHandler
    : IRequestHandler<GetStudentTaskSchemaQuery, Result<StudentTaskSchemaResponse>>
{
    private readonly IApplicationDbContext db;
    private readonly IStudentTaskAccess taskAccess;

    /// <summary>Создаёт обработчик схемы задания.</summary>
    public GetStudentTaskSchemaHandler(IApplicationDbContext db, IStudentTaskAccess taskAccess)
    {
        this.db = db;
        this.taskAccess = taskAccess;
    }

    /// <inheritdoc />
    public async Task<Result<StudentTaskSchemaResponse>> Handle(GetStudentTaskSchemaQuery query, CancellationToken ct)
    {
        var access = await taskAccess.EnsureAsync(
            query.UserId, query.ModuleSessionId, query.TaskId, PlatformScopeMode.Read, ct);
        if (!access.IsSuccess)
        {
            return Result<StudentTaskSchemaResponse>.Fail(access.Error!);
        }

        var target = await db.SqlTasks.AsNoTracking()
            .Where(task => task.Id == query.TaskId && task.PublicationStatus == PublicationStatus.Published &&
                           (query.ModuleSessionId.HasValue || task.Visibility == TaskVisibility.Open))
            .Select(task => new
            {
                task.SqlQuery.TargetDbId,
                DbName = db.TargetDbs.Where(targetDb => targetDb.Id == task.SqlQuery.TargetDbId)
                    .Select(targetDb => targetDb.DbName).First(),
                DbmsName = db.TargetDbs.Where(targetDb => targetDb.Id == task.SqlQuery.TargetDbId)
                    .Select(targetDb => targetDb.Dbms.DbmsName).First()
            })
            .FirstOrDefaultAsync(ct);
        if (target is null)
        {
            return Result<StudentTaskSchemaResponse>.Fail(StudentErrors.NotFound);
        }

        var tables = await db.MetaTables.AsNoTracking()
            .Where(table => table.TargetDbId == target.TargetDbId)
            .OrderBy(table => table.SortOrder).ThenBy(table => table.Id)
            .Select(table => new StudentSchemaTableResponse(
                table.Id,
                table.TableName,
                table.Description,
                table.Attributes.OrderBy(column => column.SortOrder).ThenBy(column => column.Id)
                    .Select(column => new StudentSchemaColumnResponse(
                        column.Id,
                        column.AttributeName,
                        column.PhysicalType.TypeName,
                        !column.IsRequired,
                        column.IsPrimaryKey))
                    .ToList()))
            .ToListAsync(ct);
        var tableIds = tables.Select(table => table.Id).ToHashSet();
        var relationships = await db.MetaRelationships.AsNoTracking()
            .Where(relationship =>
                tableIds.Contains(relationship.SourceAttribute.MetaTableId) &&
                tableIds.Contains(relationship.TargetAttribute.MetaTableId))
            .Select(relationship => new
            {
                relationship.Id,
                relationship.RelationshipName,
                relationship.SourceAttributeId,
                relationship.TargetAttributeId,
                FromTableId = relationship.SourceAttribute.MetaTableId,
                ToTableId = relationship.TargetAttribute.MetaTableId,
                SourceOrder = relationship.SourceAttribute.SortOrder
            })
            .ToListAsync(ct);
        var foreignKeys = relationships
            .GroupBy(item => new { item.RelationshipName, item.FromTableId, item.ToTableId })
            .Select(group =>
            {
                var pairs = group.OrderBy(item => item.SourceOrder).ThenBy(item => item.SourceAttributeId).ToList();
                return new StudentSchemaForeignKeyResponse(
                    pairs.Min(item => item.Id),
                    group.Key.RelationshipName,
                    group.Key.FromTableId,
                    group.Key.ToTableId,
                    pairs.Select(item => new StudentSchemaColumnPairResponse(
                        item.SourceAttributeId, item.TargetAttributeId)).ToList());
            })
            .OrderBy(key => key.Name).ThenBy(key => key.Id)
            .ToList();
        return new StudentTaskSchemaResponse(target.DbName, target.DbmsName, tables, foreignKeys);
    }
}
