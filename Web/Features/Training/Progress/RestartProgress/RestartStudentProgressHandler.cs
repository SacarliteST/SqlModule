using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Validation;
using SQLModule.Data.Core;
using SQLModule.Domain.Schema;
using SQLModule.Domain.Training;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.Progress.RestartProgress;

internal sealed record RestartStudentProgressCommand(Guid UserId, Guid TaskId, Guid IdempotencyKey)
    : IRequest<Result<StudentTaskProgressResponse>>;

internal sealed class RestartStudentProgressHandler(
    AppDbContext db,
    TimeProvider timeProvider,
    ILogger<RestartStudentProgressHandler> logger)
    : IRequestHandler<RestartStudentProgressCommand, Result<StudentTaskProgressResponse>>
{
    public async Task<Result<StudentTaskProgressResponse>> Handle(
        RestartStudentProgressCommand command,
        CancellationToken ct)
    {
        var scope = $"progress:{command.UserId:D}:restart";
        var key = command.IdempotencyKey.ToString("D");
        var hash = ProgressIdempotency.Hash(command.TaskId);
        var receipt = await db.MutationReceipts.AsNoTracking()
            .SingleOrDefaultAsync(value => value.Scope == scope && value.IdempotencyKey == key, ct);
        if (receipt is not null)
        {
            return ProgressIdempotency.Replay(receipt, hash);
        }

        var task = await db.SqlTasks.AsNoTracking()
            .Where(value => value.Id == command.TaskId &&
                            value.PublicationStatus == PublicationStatus.Published)
            .Select(value => new { value.ActiveValidationVersionId, value.ActiveValidationVersion })
            .SingleOrDefaultAsync(ct);
        if (task is null)
        {
            return Result<StudentTaskProgressResponse>.Fail(ProgressErrors.TaskNotFound(command.TaskId));
        }

        if (task.ActiveValidationVersionId is null || task.ActiveValidationVersion is null)
        {
            return Result<StudentTaskProgressResponse>.Fail(ProgressErrors.ValidationVersionNotPublished);
        }

        var previous = await db.StudentTaskProgresses
            .Include(value => value.ValidationVersion)
            .Where(value => value.UserId == command.UserId && value.TaskId == command.TaskId &&
                            value.ModuleSessionId == null)
            .OrderByDescending(value => value.CreatedAt)
            .FirstOrDefaultAsync(ct);
        if (previous is null)
        {
            return Result<StudentTaskProgressResponse>.Fail(ProgressErrors.NotFound);
        }

        var previousHasAttempts = !previous.ValidationVersion.MaxAttempts.HasValue ||
                                  previous.AttemptsUsed < previous.ValidationVersion.MaxAttempts.Value;
        if (previous.Status == ProgressStatus.Active && previousHasAttempts)
        {
            return Result<StudentTaskProgressResponse>.Fail(ProgressErrors.StillActive);
        }

        var now = timeProvider.GetUtcNow();
        if (previous.Status is ProgressStatus.Active or ProgressStatus.Finalizing or
            ProgressStatus.CompletionPending or ProgressStatus.CompletionFailed)
        {
            previous.CloseForRestart(now);
        }

        var progress = StudentTaskProgress.CreateStandalone(
            command.UserId, command.TaskId, task.ActiveValidationVersionId.Value);
        var response = ProgressMappings.ToResponse(progress, task.ActiveValidationVersion, now);
        db.StudentTaskProgresses.Add(progress);
        db.MutationReceipts.Add(MutationReceipt.Create(
            scope, key, hash, ProgressIdempotency.Serialize(response)));

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            var concurrentReceipt = await db.MutationReceipts.AsNoTracking()
                .SingleOrDefaultAsync(value => value.Scope == scope && value.IdempotencyKey == key, ct);
            if (concurrentReceipt is not null)
            {
                return ProgressIdempotency.Replay(concurrentReceipt, hash);
            }

            throw;
        }

        logger.LogInformation(
            "Студент {UserId} перезапустил standalone-прохождение задания {TaskId}: {OldProgressId} -> {ProgressId}",
            command.UserId, command.TaskId, previous.Id, progress.Id);
        return response;
    }
}
