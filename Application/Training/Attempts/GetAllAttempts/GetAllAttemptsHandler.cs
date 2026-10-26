using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Attempt;
using SQLModule.Application.Persistence;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.Training.Attempts;

public sealed class GetAllAttemptsHandler
    : IRequestHandler<GetAllAttemptsQuery, Result<PageResponse<AttemptListItemResponse>>>
{
    private readonly IApplicationDbContext db;

    /// <summary>Создаёт обработчик журнала попыток преподавателя.</summary>
    public GetAllAttemptsHandler(IApplicationDbContext db)
    {
        this.db = db;
    }

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

        if (query.ValidationVersionId.HasValue)
        {
            q = q.Where(x => x.ValidationVersionId == query.ValidationVersionId.Value);
        }

        if (query.ScoreFrom.HasValue)
        {
            q = q.Where(x => x.Score >= query.ScoreFrom.Value);
        }

        if (query.ScoreTo.HasValue)
        {
            q = q.Where(x => x.Score <= query.ScoreTo.Value);
        }

        if (query.FinalizationReason.HasValue)
        {
            q = q.Where(x =>
                x.ModuleSessionId.HasValue && db.ModuleSessions.Any(session =>
                    session.Id == x.ModuleSessionId.Value &&
                    session.FinalizationReason == query.FinalizationReason.Value));
        }

        if (query.Origin.HasValue)
        {
            q = query.Origin == AttemptOrigin.Training
                ? q.Where(attempt => attempt.ModuleSessionId == null)
                : q.Where(attempt => attempt.ModuleSessionId != null);
        }

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(a => a.StartedAt)
            .ThenByDescending(a => a.Id)
            .Skip(query.Offset)
            .Take(query.Limit)
            .Select(x => new AttemptListItemResponse(
                x.Id, x.UserId, x.StudentName, x.TaskId, x.SubmittedSql, x.Status.ToContract(),
                x.IsCorrect, x.Reason.ToContract(), x.RowCount, x.DurationMs,
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
                x.UpdatedByName ?? x.StudentName,
                x.ValidationVersionId,
                x.AttemptNumber,
                x.Score,
                x.ModuleSessionId == null ? AttemptOrigin.Training : AttemptOrigin.Platform))
            .ToListAsync(ct);

        return Result<PageResponse<AttemptListItemResponse>>.Success(new PageResponse<AttemptListItemResponse>
        {
            Items = items,
            Count = total
        });
    }
}
