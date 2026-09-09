using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Attempt;
using SQLModule.Data.Core;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.Attempts;

internal record GetAllAttemptsQuery(
    int Offset, int Limit, Guid? TaskId, Guid? UserId, Guid? TopicId,
    Domain.Training.ExecutionStatus? Status, bool? IsCorrect,
    DateTimeOffset? DateFrom, DateTimeOffset? DateTo)
    : IRequest<Result<PageResponse<AttemptListItemResponse>>>;

internal sealed class GetAllAttemptsHandler(AppDbContext db)
    : IRequestHandler<GetAllAttemptsQuery, Result<PageResponse<AttemptListItemResponse>>>
{
    public async Task<Result<PageResponse<AttemptListItemResponse>>> Handle(
        GetAllAttemptsQuery query, CancellationToken ct)
    {
        var q = db.Attempts.AsNoTracking();

        if (query.TaskId.HasValue)
        {
            q = q.Where(a => a.TaskId == query.TaskId.Value);
        }

        if (query.UserId.HasValue)
        {
            q = q.Where(a => a.UserId == query.UserId.Value);
        }

        if (query.TopicId.HasValue)
        {
            q = q.Where(x => db.SqlTasks.Any(task => task.Id == x.TaskId && task.TopicId == query.TopicId.Value));
        }

        if (query.Status.HasValue)
        {
            q = q.Where(x => x.Status == query.Status.Value);
        }

        if (query.IsCorrect.HasValue)
        {
            q = q.Where(x => x.IsCorrect == query.IsCorrect.Value);
        }

        if (query.DateFrom.HasValue)
        {
            q = q.Where(x => x.StartedAt >= query.DateFrom.Value);
        }

        if (query.DateTo.HasValue)
        {
            q = q.Where(x => x.StartedAt <= query.DateTo.Value);
        }

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(a => a.StartedAt)
            .Skip(query.Offset)
            .Take(query.Limit)
            .Select(x => new AttemptListItemResponse(
                x.Id, x.UserId, x.StudentName, x.TaskId, x.SubmittedSql, x.Status,
                x.IsCorrect, x.Reason, x.RowCount, x.DurationMs,
                x.Status == Domain.Training.ExecutionStatus.Error ||
                x.Status == Domain.Training.ExecutionStatus.TimedOut
                    ? "Не удалось выполнить SQL-запрос."
                    : null,
                x.StartedAt, x.FinishedAt,
                x.CreatedById == Guid.Empty ? x.UserId : x.CreatedById,
                x.StartedAt,
                x.UpdatedById == Guid.Empty ? x.UserId : x.UpdatedById,
                x.FinishedAt,
                db.SqlTasks.Where(task => task.Id == x.TaskId).Select(task => task.TaskName).First(),
                db.SqlTasks.Where(task => task.Id == x.TaskId).Select(task => task.Topic.TopicName).First(),
                x.Status == Domain.Training.ExecutionStatus.Error ||
                x.Status == Domain.Training.ExecutionStatus.TimedOut
                    ? "Не удалось выполнить SQL-запрос."
                    : null,
                x.CreatedByName ?? x.StudentName,
                x.UpdatedByName ?? x.StudentName))
            .ToListAsync(ct);

        return Result<PageResponse<AttemptListItemResponse>>.Success(new PageResponse<AttemptListItemResponse>
        {
            Items = items,
            Count = total
        });
    }
}
