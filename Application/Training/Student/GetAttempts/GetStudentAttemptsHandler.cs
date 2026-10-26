using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Persistence;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Student;

namespace SQLModule.Application.Training.Student;

/// <summary>Возвращает только собственные попытки текущего студента.</summary>
public sealed class GetStudentAttemptsHandler
    : IRequestHandler<GetStudentAttemptsQuery, Result<PageResponse<StudentAttemptListItemResponse>>>
{
    private readonly IApplicationDbContext db;

    /// <summary>Создаёт обработчик личной истории попыток.</summary>
    public GetStudentAttemptsHandler(IApplicationDbContext db)
    {
        this.db = db;
    }

    /// <inheritdoc />
    public async Task<Result<PageResponse<StudentAttemptListItemResponse>>> Handle(
        GetStudentAttemptsQuery query,
        CancellationToken ct)
    {
        var owned = OwnedAttempts(query.Request, query.UserId);
        var count = await owned.CountAsync(ct);
        var items = await MapAttemptList(owned.OrderByDescending(attempt => attempt.StartedAt)
                .ThenByDescending(attempt => attempt.Id))
            .Skip(query.Request.Offset)
            .Take(query.Request.Limit)
            .ToListAsync(ct);
        return new PageResponse<StudentAttemptListItemResponse> { Items = items, Count = count };
    }

    private IQueryable<Domain.Training.Attempt> OwnedAttempts(StudentAttemptsRequest request, Guid userId)
    {
        var query = db.Attempts.AsNoTracking().Where(attempt => attempt.UserId == userId);
        if (request.TaskId.HasValue)
        {
            query = query.Where(attempt => attempt.TaskId == request.TaskId);
        }

        if (request.TopicId.HasValue)
        {
            query = query.Where(attempt => db.SqlTasks.Any(task =>
                task.Id == attempt.TaskId && task.TopicId == request.TopicId));
        }

        if (request.Status.HasValue)
        {
            var status = request.Status.Value.ToDomain();
            query = query.Where(attempt => attempt.Status == status);
        }

        if (request.IsCorrect.HasValue)
        {
            query = query.Where(attempt => attempt.IsCorrect == request.IsCorrect);
        }

        if (request.DateFrom.HasValue)
        {
            query = query.Where(attempt => attempt.StartedAt >= request.DateFrom);
        }

        if (request.DateTo.HasValue)
        {
            query = query.Where(attempt => attempt.StartedAt <= request.DateTo);
        }

        return query;
    }

    private IQueryable<StudentAttemptListItemResponse> MapAttemptList(
        IQueryable<Domain.Training.Attempt> query) =>
        query.Select(attempt => new StudentAttemptListItemResponse(
            attempt.Id,
            attempt.TaskId,
            db.SqlTasks.Where(task => task.Id == attempt.TaskId).Select(task => task.TaskName).First(),
            db.SqlTasks.Where(task => task.Id == attempt.TaskId).Select(task => task.TopicId).First(),
            db.SqlTasks.Where(task => task.Id == attempt.TaskId).Select(task => task.Topic.TopicName).First(),
            attempt.SubmittedSql,
            attempt.Status.ToContract(),
            attempt.IsCorrect,
            attempt.Reason.ToContract(),
            attempt.RowCount,
            attempt.DurationMs,
            attempt.ErrorMessage == null ? null : "SQL-запрос не удалось выполнить.",
            attempt.StartedAt,
            attempt.FinishedAt,
            attempt.AttemptNumber,
            attempt.Score,
            attempt.ValidationVersionId));
}
