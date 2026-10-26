using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Persistence;
using SQLModule.Application.Training.Attempts;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Student;

namespace SQLModule.Application.Training.Student;

/// <summary>Возвращает безопасные подробности только собственной попытки студента.</summary>
public sealed class GetStudentAttemptByIdHandler
    : IRequestHandler<GetStudentAttemptByIdQuery, Result<StudentAttemptResponse>>
{
    private readonly IApplicationDbContext db;
    private readonly IAttemptResultSnapshotService snapshotService;
    private readonly IAttemptScoringReadService scoringReadService;
    private readonly TimeProvider timeProvider;

    /// <summary>Создаёт обработчик подробностей попытки.</summary>
    public GetStudentAttemptByIdHandler(
        IApplicationDbContext db,
        IAttemptResultSnapshotService snapshotService,
        IAttemptScoringReadService scoringReadService,
        TimeProvider timeProvider)
    {
        this.db = db;
        this.snapshotService = snapshotService;
        this.scoringReadService = scoringReadService;
        this.timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<Result<StudentAttemptResponse>> Handle(GetStudentAttemptByIdQuery query, CancellationToken ct)
    {
        var attempt = await db.Attempts.AsNoTracking()
            .FirstOrDefaultAsync(value => value.Id == query.AttemptId && value.UserId == query.UserId, ct);
        if (attempt is null)
        {
            return Result<StudentAttemptResponse>.Fail(StudentErrors.NotFound);
        }

        var task = await db.SqlTasks.AsNoTracking()
            .Where(value => value.Id == attempt.TaskId)
            .Select(value => new { value.TaskName, value.TopicId, value.Topic.TopicName })
            .SingleAsync(ct);
        var snapshot = snapshotService.Read(attempt, timeProvider.GetUtcNow());
        var scoring = await scoringReadService.ReadAsync(attempt, studentSafe: true, ct);
        if (scoring is not null && !attempt.ModuleSessionId.HasValue)
        {
            scoring = scoring with { Checks = [], Hints = [] };
        }

        return new StudentAttemptResponse(
            attempt.Id, attempt.TaskId, task.TaskName, task.TopicId, task.TopicName,
            attempt.SubmittedSql, attempt.Status.ToContract(), attempt.IsCorrect, attempt.Reason.ToContract(),
            attempt.RowCount, attempt.DurationMs, AttemptMappings.ToPublicError(attempt),
            attempt.StartedAt, attempt.FinishedAt,
            snapshot.State.ToContract(), snapshot.Columns, snapshot.Rows, snapshot.ReturnedRowCount,
            snapshot.IsTruncated, snapshot.RowLimit, snapshot.CreatedAt, snapshot.ExpiresAt,
            scoring);
    }
}
