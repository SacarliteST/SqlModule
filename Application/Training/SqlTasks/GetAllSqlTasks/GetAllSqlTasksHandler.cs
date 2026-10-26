using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.SqlTask;
using SQLModule.Application.Persistence;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.Training.SqlTasks;

public sealed class GetAllSqlTasksHandler
    : IRequestHandler<GetAllSqlTasksQuery, Result<PageResponse<SqlTaskResponse>>>
{
    private readonly IApplicationDbContext db;

    /// <summary>Создаёт обработчик списка SQL-заданий.</summary>
    public GetAllSqlTasksHandler(IApplicationDbContext db)
    {
        this.db = db;
    }

    public async Task<Result<PageResponse<SqlTaskResponse>>> Handle(
        GetAllSqlTasksQuery query, CancellationToken ct)
    {
        var queryable = db.SqlTasks.AsNoTracking();
        if (query.TopicId.HasValue)
        {
            queryable = queryable.Where(x => x.TopicId == query.TopicId.Value);
        }

        if (!String.IsNullOrWhiteSpace(query.Name))
        {
            queryable = queryable.Where(x => x.TaskName.Contains(query.Name));
        }

        if (query.TargetDbId.HasValue)
        {
            queryable = queryable.Where(x => x.SqlQuery.TargetDbId == query.TargetDbId.Value);
        }

        if (query.DifficultyLevel.HasValue)
        {
            queryable = queryable.Where(x => x.DifficultyLevel == query.DifficultyLevel.Value);
        }

        if (query.PublicationStatus.HasValue)
        {
            queryable = queryable.Where(x => x.PublicationStatus == query.PublicationStatus.Value);
        }

        var total = await queryable.CountAsync(ct);

        var items = await queryable
            .OrderBy(x => x.DifficultyLevel)
            .ThenBy(x => x.TaskName)
            .Skip(query.Offset)
            .Take(query.Limit)
            .Select(x => new SqlTaskResponse(
                x.Id, x.TopicId, x.SqlQueryId, x.TaskName, x.TaskText, x.DifficultyLevel,
                x.PublicationStatus.ToContract(), x.CreatedById, x.CreatedAt, x.UpdatedById, x.UpdatedAt,
                x.Topic.TopicName, x.SqlQuery.TargetDbId,
                db.TargetDbs.Where(target => target.Id == x.SqlQuery.TargetDbId)
                    .Select(target => target.DbName).First(),
                db.TargetDbs.Where(target => target.Id == x.SqlQuery.TargetDbId)
                    .Select(target => target.Dbms.DbmsName).First(),
                db.Attempts.Count(attempt => attempt.TaskId == x.Id),
                x.CreatedByName,
                x.UpdatedByName,
                x.Visibility.ToContract(),
                x.BankNumber,
                x.BankPublishedAt))
            .ToListAsync(ct);

        return Result<PageResponse<SqlTaskResponse>>.Success(new PageResponse<SqlTaskResponse>
        {
            Items = items,
            Count = total
        });
    }
}
