using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Persistence;
using SQLModule.Application.Schema.SchemaBuilder;
using SQLModule.Application.Schema.SchemaBuilder.GetTableRows;
using SQLModule.Application.Training.Attempts;
using SQLModule.Application.Training.Progress;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Contracts.Training.Student;
using SQLModule.Contracts.Training.Validation;
using SQLModule.Domain.Training;
using SQLModule.Sandbox;

namespace SQLModule.Application.Training.Student;

/// <summary>Ошибки студенческих запросов; эндпоинты отвечают на них пустым 404, как раньше.</summary>
public static class StudentErrors
{
    public static Error NotFound { get; } = new("Student.NotFound", "Ресурс не найден.", ErrorType.NotFound);
}

public record GetStudentTopicsQuery(int Offset, int Limit) : IRequest<Result<PageResponse<StudentTopicResponse>>>;

public record GetStudentTasksQuery(StudentTasksRequest Request) : IRequest<Result<PageResponse<StudentTaskResponse>>>;

public record GetStudentTaskByIdQuery(Guid TaskId, Guid UserId, Guid? ModuleSessionId)
    : IRequest<Result<StudentTaskDetailsResponse>>;

public record GetStudentTaskSchemaQuery(Guid TaskId) : IRequest<Result<StudentTaskSchemaResponse>>;

public record GetStudentTaskTableRowsQuery(Guid TaskId, Guid TableId, int Offset, int Limit)
    : IRequest<Result<TableRowsResponse>>;

public record GetStudentAttemptsQuery(StudentAttemptsRequest Request, Guid UserId)
    : IRequest<Result<PageResponse<StudentAttemptListItemResponse>>>;

public record GetStudentAttemptByIdQuery(Guid AttemptId, Guid UserId) : IRequest<Result<StudentAttemptResponse>>;

public sealed class GetStudentTopicsHandler(IApplicationDbContext db)
    : IRequestHandler<GetStudentTopicsQuery, Result<PageResponse<StudentTopicResponse>>>
{
    public async Task<Result<PageResponse<StudentTopicResponse>>> Handle(
        GetStudentTopicsQuery request, CancellationToken ct)
    {
        var topics = await db.Topics.AsNoTracking()
            .Select(t => new { t.Id, t.TopicName, t.ParentTopicId, t.Description })
            .ToListAsync(ct);
        var counts = await db.SqlTasks.AsNoTracking()
            .Where(t => t.PublicationStatus == PublicationStatus.Published)
            .GroupBy(t => t.TopicId).Select(g => new { TopicId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TopicId, x => x.Count, ct);

        var byId = topics.ToDictionary(x => x.Id);
        var visible = counts.Keys.ToHashSet();
        foreach (var topicId in counts.Keys)
        {
            var current = byId.GetValueOrDefault(topicId);
            while (current?.ParentTopicId is Guid parentId && visible.Add(parentId))
            {
                current = byId.GetValueOrDefault(parentId);
            }
        }

        var filtered = topics.Where(t => visible.Contains(t.Id))
            .OrderBy(t => t.TopicName).ThenBy(t => t.Id).ToList();
        var items = filtered.Skip(request.Offset).Take(request.Limit)
            .Select(t => new StudentTopicResponse(t.Id, t.TopicName, t.ParentTopicId, t.Description,
                counts.GetValueOrDefault(t.Id))).ToList();
        return new PageResponse<StudentTopicResponse> { Items = items, Count = filtered.Count };
    }
}

public sealed class GetStudentTasksHandler(IApplicationDbContext db)
    : IRequestHandler<GetStudentTasksQuery, Result<PageResponse<StudentTaskResponse>>>
{
    public async Task<Result<PageResponse<StudentTaskResponse>>> Handle(
        GetStudentTasksQuery command, CancellationToken ct)
    {
        var request = command.Request;
        var query = db.SqlTasks.AsNoTracking().Where(t => t.PublicationStatus == PublicationStatus.Published);
        if (request.TopicId.HasValue)
        {
            query = query.Where(t => t.TopicId == request.TopicId);
        }

        if (!String.IsNullOrWhiteSpace(request.Name))
        {
            query = query.Where(t => t.TaskName.Contains(request.Name));
        }

        if (request.DifficultyLevel.HasValue)
        {
            query = query.Where(t => t.DifficultyLevel == request.DifficultyLevel);
        }

        var count = await query.CountAsync(ct);
        var items = await query.OrderBy(t => t.DifficultyLevel).ThenBy(t => t.TaskName).ThenBy(t => t.Id)
            .Skip(request.Offset).Take(request.Limit)
            .Select(t => new StudentTaskResponse(t.Id, t.TopicId, t.Topic.TopicName, t.TaskName,
                t.TaskText.Length <= 200 ? t.TaskText : t.TaskText.Substring(0, 200), t.DifficultyLevel,
                db.TargetDbs.Where(d => d.Id == t.SqlQuery.TargetDbId).Select(d => d.Dbms.DbmsName).First()))
            .ToListAsync(ct);
        return new PageResponse<StudentTaskResponse> { Items = items, Count = count };
    }
}

public sealed class GetStudentTaskByIdHandler(
    IApplicationDbContext db,
    IOptions<SandboxOptions> options,
    TimeProvider timeProvider)
    : IRequestHandler<GetStudentTaskByIdQuery, Result<StudentTaskDetailsResponse>>
{
    public async Task<Result<StudentTaskDetailsResponse>> Handle(GetStudentTaskByIdQuery query, CancellationToken ct)
    {
        var taskId = query.TaskId;
        var o = options.Value;
        var item = await db.SqlTasks.AsNoTracking()
            .Where(t => t.Id == taskId && t.PublicationStatus == PublicationStatus.Published)
            .Select(t => new
            {
                t.Id,
                t.TopicId,
                t.Topic.TopicName,
                t.TaskName,
                t.TaskText,
                t.DifficultyLevel,
                t.ActiveValidationVersionId,
                DbmsName = db.TargetDbs.Where(d => d.Id == t.SqlQuery.TargetDbId)
                    .Select(d => d.Dbms.DbmsName).First()
            })
            .FirstOrDefaultAsync(ct);
        if (item is null)
        {
            return Result<StudentTaskDetailsResponse>.Fail(StudentErrors.NotFound);
        }

        StudentTaskValidationResponse? validation = null;
        if (item.ActiveValidationVersionId.HasValue)
        {
            var version = await db.TaskValidationVersions.AsNoTracking()
                .SingleOrDefaultAsync(value => value.Id == item.ActiveValidationVersionId.Value, ct);
            if (version is not null)
            {
                var sessionId = query.ModuleSessionId;
                var progress = await db.StudentTaskProgresses.AsNoTracking()
                    .Where(value => value.TaskId == taskId && value.UserId == query.UserId &&
                                    value.ModuleSessionId == sessionId)
                    .OrderByDescending(value => value.CreatedAt)
                    .FirstOrDefaultAsync(ct);
                validation = new StudentTaskValidationResponse(
                    version.PassingScore,
                    version.MaxAttempts,
                    progress is null ? null : ProgressMappings.ToResponse(progress, version, timeProvider.GetUtcNow()),
                    StudentHints.Build(version));
            }
        }

        return new StudentTaskDetailsResponse(
            item.Id,
            item.TopicId,
            item.TopicName,
            item.TaskName,
            item.TaskText,
            item.DifficultyLevel,
            item.DbmsName,
            new StudentExecutionLimitsResponse(o.DefaultQueryTimeoutSeconds, o.MaxRows, o.MaxSqlLength),
            validation);
    }
}

public sealed class GetStudentTaskSchemaHandler(IApplicationDbContext db)
    : IRequestHandler<GetStudentTaskSchemaQuery, Result<StudentTaskSchemaResponse>>
{
    public async Task<Result<StudentTaskSchemaResponse>> Handle(GetStudentTaskSchemaQuery query, CancellationToken ct)
    {
        var taskId = query.TaskId;
        var target = await db.SqlTasks.AsNoTracking()
            .Where(task => task.Id == taskId && task.PublicationStatus == PublicationStatus.Published)
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
                var orderedPairs = group.OrderBy(item => item.SourceOrder).ThenBy(item => item.SourceAttributeId).ToList();
                return new StudentSchemaForeignKeyResponse(
                    orderedPairs.Min(item => item.Id),
                    group.Key.RelationshipName,
                    group.Key.FromTableId,
                    group.Key.ToTableId,
                    orderedPairs.Select(item => new StudentSchemaColumnPairResponse(
                        item.SourceAttributeId, item.TargetAttributeId)).ToList());
            })
            .OrderBy(key => key.Name).ThenBy(key => key.Id)
            .ToList();

        return new StudentTaskSchemaResponse(target.DbName, target.DbmsName, tables, foreignKeys);
    }
}

public sealed class GetStudentTaskTableRowsHandler(IApplicationDbContext db, ISender sender)
    : IRequestHandler<GetStudentTaskTableRowsQuery, Result<TableRowsResponse>>
{
    public async Task<Result<TableRowsResponse>> Handle(GetStudentTaskTableRowsQuery query, CancellationToken ct)
    {
        var taskId = query.TaskId;
        var targetDbId = await db.SqlTasks.AsNoTracking()
            .Where(task => task.Id == taskId && task.PublicationStatus == PublicationStatus.Published)
            .Select(task => (Guid?)task.SqlQuery.TargetDbId)
            .SingleOrDefaultAsync(ct);

        if (!targetDbId.HasValue)
        {
            return Result<TableRowsResponse>.Fail(StudentErrors.NotFound);
        }

        return await sender.Send<GetTableRowsQuery, Result<TableRowsResponse>>(
            new GetTableRowsQuery(targetDbId.Value, query.TableId, query.Offset, query.Limit), ct);
    }
}

public sealed class GetStudentAttemptsHandler(IApplicationDbContext db)
    : IRequestHandler<GetStudentAttemptsQuery, Result<PageResponse<StudentAttemptListItemResponse>>>
{
    public async Task<Result<PageResponse<StudentAttemptListItemResponse>>> Handle(
        GetStudentAttemptsQuery query, CancellationToken ct)
    {
        var request = query.Request;
        var owned = OwnedAttempts(request, query.UserId);
        var count = await owned.CountAsync(ct);
        var items = await MapAttemptList(owned.OrderByDescending(a => a.StartedAt).ThenByDescending(a => a.Id))
            .Skip(request.Offset).Take(request.Limit).ToListAsync(ct);
        return new PageResponse<StudentAttemptListItemResponse> { Items = items, Count = count };
    }

    private IQueryable<Domain.Training.Attempt> OwnedAttempts(StudentAttemptsRequest request, Guid userId)
    {
        var query = db.Attempts.AsNoTracking().Where(a => a.UserId == userId);
        if (request.TaskId.HasValue)
        {
            query = query.Where(a => a.TaskId == request.TaskId);
        }

        if (request.TopicId.HasValue)
        {
            query = query.Where(a => db.SqlTasks.Any(t => t.Id == a.TaskId && t.TopicId == request.TopicId));
        }

        if (request.Status.HasValue)
        {
            query = query.Where(a => a.Status == request.Status);
        }

        if (request.IsCorrect.HasValue)
        {
            query = query.Where(a => a.IsCorrect == request.IsCorrect);
        }

        if (request.DateFrom.HasValue)
        {
            query = query.Where(a => a.StartedAt >= request.DateFrom);
        }

        if (request.DateTo.HasValue)
        {
            query = query.Where(a => a.StartedAt <= request.DateTo);
        }

        return query;
    }

    private IQueryable<StudentAttemptListItemResponse> MapAttemptList(IQueryable<Domain.Training.Attempt> query) =>
        query.Select(a => new StudentAttemptListItemResponse(a.Id, a.TaskId,
            db.SqlTasks.Where(t => t.Id == a.TaskId).Select(t => t.TaskName).First(),
            db.SqlTasks.Where(t => t.Id == a.TaskId).Select(t => t.TopicId).First(),
            db.SqlTasks.Where(t => t.Id == a.TaskId).Select(t => t.Topic.TopicName).First(),
            a.SubmittedSql, a.Status, a.IsCorrect, a.Reason, a.RowCount, a.DurationMs,
            a.ErrorMessage == null ? null : "SQL-запрос не удалось выполнить.", a.StartedAt, a.FinishedAt,
            a.AttemptNumber, a.Score, a.ProgressId, a.ValidationVersionId));
}

public sealed class GetStudentAttemptByIdHandler(
    IApplicationDbContext db,
    IAttemptResultSnapshotService snapshotService,
    IAttemptScoringReadService scoringReadService,
    TimeProvider timeProvider)
    : IRequestHandler<GetStudentAttemptByIdQuery, Result<StudentAttemptResponse>>
{
    public async Task<Result<StudentAttemptResponse>> Handle(GetStudentAttemptByIdQuery query, CancellationToken ct)
    {
        var attempt = await db.Attempts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == query.AttemptId && a.UserId == query.UserId, ct);
        if (attempt is null)
        {
            return Result<StudentAttemptResponse>.Fail(StudentErrors.NotFound);
        }

        var task = await db.SqlTasks.AsNoTracking()
            .Where(t => t.Id == attempt.TaskId)
            .Select(t => new { t.TaskName, t.TopicId, t.Topic.TopicName })
            .SingleAsync(ct);
        var snapshot = snapshotService.Read(attempt, timeProvider.GetUtcNow());
        var scoring = await scoringReadService.ReadAsync(attempt, studentSafe: true, ct);
        return new StudentAttemptResponse(
            attempt.Id, attempt.TaskId, task.TaskName, task.TopicId, task.TopicName,
            attempt.SubmittedSql, attempt.Status, attempt.IsCorrect, attempt.Reason,
            attempt.RowCount, attempt.DurationMs, AttemptMappings.ToPublicError(attempt),
            attempt.StartedAt, attempt.FinishedAt,
            snapshot.State, snapshot.Columns, snapshot.Rows, snapshot.ReturnedRowCount,
            snapshot.IsTruncated, snapshot.RowLimit, snapshot.CreatedAt, snapshot.ExpiresAt,
            scoring);
    }
}

/// <summary>Подсказки студенту, собранные из снимка версии проверки.</summary>
internal static class StudentHints
{
    internal static StudentTaskHintsResponse Build(TaskValidationVersion version)
    {
        var groups = version.GetVisibleHintGroups();
        try
        {
            var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            var configuration = JsonSerializer.Deserialize<ValidationConfigurationSnapshot>(
                version.ValidationConfigurationSnapshotJson, jsonOptions);
            var schema = JsonSerializer.Deserialize<SchemaSpec>(version.SchemaSnapshotJson, jsonOptions);
            if (configuration is null || schema is null)
            {
                return EmptyHints(groups);
            }

            var visible = groups.ToHashSet();
            var requiredConstructs = visible.Contains(HintGroup.RequiredConstructs)
                ? ParseConstructs(configuration.Checks, ValidationCheckKind.RequiredConstruct)
                : [];
            var forbiddenConstructs = visible.Contains(HintGroup.ForbiddenConstructs)
                ? ParseConstructs(configuration.Checks, ValidationCheckKind.ForbiddenConstruct)
                : [];
            var tables = schema.Tables
                .Where(value => Guid.TryParse(value.Key, out _))
                .ToDictionary(value => value.Key, value => value.Name);
            var requiredTables = visible.Contains(HintGroup.RequiredTables)
                ? ParseTables(configuration.Checks, ValidationCheckKind.RequiredTable, tables)
                : [];
            var forbiddenTables = visible.Contains(HintGroup.ForbiddenTables)
                ? ParseTables(configuration.Checks, ValidationCheckKind.ForbiddenTable, tables)
                : [];
            return new StudentTaskHintsResponse(
                groups, requiredConstructs, forbiddenConstructs, requiredTables, forbiddenTables);
        }
        catch (JsonException)
        {
            return EmptyHints(groups);
        }
    }

    private static IReadOnlyList<SqlConstruct> ParseConstructs(
        IEnumerable<ValidationCheckSnapshot> checks,
        ValidationCheckKind kind) =>
        checks.Where(value => value.Kind == kind)
            .Select(value => Enum.TryParse<SqlConstruct>(value.Value, out var construct)
                ? (SqlConstruct?)construct
                : null)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .Distinct()
            .ToArray();

    private static IReadOnlyList<StudentHintTableResponse> ParseTables(
        IEnumerable<ValidationCheckSnapshot> checks,
        ValidationCheckKind kind,
        IReadOnlyDictionary<string, string> tables) =>
        checks.Where(value => value.Kind == kind && value.Value is not null && tables.ContainsKey(value.Value))
            .Select(value => new StudentHintTableResponse(Guid.Parse(value.Value!), tables[value.Value!]))
            .DistinctBy(value => value.Id)
            .ToArray();

    private static StudentTaskHintsResponse EmptyHints(IReadOnlyList<HintGroup> groups) =>
        new(groups, [], [], [], []);
}
