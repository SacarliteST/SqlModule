using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.SqlTask;
using SQLModule.Data.Core;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.SqlTasks;

internal record GetAllSqlTasksQuery(
    int Offset, int Limit, Guid? TopicId, string? Name, Guid? TargetDbId,
    short? DifficultyLevel, Domain.Training.PublicationStatus? PublicationStatus)
    : IRequest<Result<PageResponse<SqlTaskResponse>>>;

internal sealed class GetAllSqlTasksHandler(AppDbContext db)
    : IRequestHandler<GetAllSqlTasksQuery, Result<PageResponse<SqlTaskResponse>>>
{
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
                x.PublicationStatus, x.CreatedById, x.CreatedAt, x.UpdatedById, x.UpdatedAt,
                x.Topic.TopicName, x.SqlQuery.TargetDbId,
                db.TargetDbs.Where(target => target.Id == x.SqlQuery.TargetDbId)
                    .Select(target => target.DbName).First(),
                db.TargetDbs.Where(target => target.Id == x.SqlQuery.TargetDbId)
                    .Select(target => target.Dbms.DbmsName).First(),
                db.Attempts.Count(attempt => attempt.TaskId == x.Id),
                x.CreatedByName,
                x.UpdatedByName))
            .ToListAsync(ct);

        return Result<PageResponse<SqlTaskResponse>>.Success(new PageResponse<SqlTaskResponse>
        {
            Items = items,
            Count = total
        });
    }
}
