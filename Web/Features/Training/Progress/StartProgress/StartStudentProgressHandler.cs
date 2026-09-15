using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Validation;
using SQLModule.Data.Core;
using SQLModule.Domain.Schema;
using SQLModule.Domain.Training;
using SQLModule.Web.Common.Cqrs;
using SQLModule.Web.Features.ModuleIntegration;

namespace SQLModule.Web.Features.Training.Progress.StartProgress;

internal sealed record StartStudentProgressCommand(Guid UserId, Guid TaskId, Guid IdempotencyKey)
    : IRequest<Result<StudentTaskProgressResponse>>;

internal sealed class StartStudentProgressHandler(
    AppDbContext db,
    IOptions<ModuleIntegrationOptions> integrationOptions,
    TimeProvider timeProvider,
    ILogger<StartStudentProgressHandler> logger)
    : IRequestHandler<StartStudentProgressCommand, Result<StudentTaskProgressResponse>>
{
    public async Task<Result<StudentTaskProgressResponse>> Handle(
        StartStudentProgressCommand command,
        CancellationToken ct)
    {
        if (integrationOptions.Value.Enabled)
        {
            return Result<StudentTaskProgressResponse>.Fail(ProgressErrors.PlatformFlowRequired);
        }

        var scope = $"progress:{command.UserId:D}:start";
        var key = command.IdempotencyKey.ToString("D");
        var hash = ProgressIdempotency.Hash(command.TaskId);
        var receipt = await db.MutationReceipts.AsNoTracking()
            .SingleOrDefaultAsync(value => value.Scope == scope && value.IdempotencyKey == key, ct);
        if (receipt is not null)
        {
            return ProgressIdempotency.Replay(receipt, hash);
        }

        var version = await ActiveVersionAsync(command.TaskId, ct);
        if (version is null)
        {
            return await TaskExistsAsync(command.TaskId, ct)
                ? Result<StudentTaskProgressResponse>.Fail(ProgressErrors.ValidationVersionNotPublished)
                : Result<StudentTaskProgressResponse>.Fail(ProgressErrors.TaskNotFound(command.TaskId));
        }

        var progress = await db.StudentTaskProgresses
            .Include(value => value.ValidationVersion)
            .SingleOrDefaultAsync(value =>
                value.UserId == command.UserId && value.TaskId == command.TaskId &&
                value.ModuleSessionId == null &&
                (value.Status == ProgressStatus.Active ||
                 value.Status == ProgressStatus.Finalizing ||
                 value.Status == ProgressStatus.CompletionPending ||
                 value.Status == ProgressStatus.CompletionFailed), ct);
        progress ??= StudentTaskProgress.CreateStandalone(command.UserId, command.TaskId, version.Id);
        if (db.Entry(progress).State == EntityState.Detached)
        {
            db.StudentTaskProgresses.Add(progress);
        }

        var response = ProgressMappings.ToResponse(
            progress,
            progress.ValidationVersion ?? version,
            timeProvider.GetUtcNow());
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
            "Студент {UserId} начал standalone-прохождение {ProgressId} задания {TaskId}",
            command.UserId, progress.Id, command.TaskId);
        return response;
    }

    private Task<TaskValidationVersion?> ActiveVersionAsync(Guid taskId, CancellationToken ct) =>
        db.SqlTasks.AsNoTracking()
            .Where(task => task.Id == taskId && task.PublicationStatus == PublicationStatus.Published &&
                           task.ActiveValidationVersionId != null)
            .Select(task => task.ActiveValidationVersion)
            .SingleOrDefaultAsync(ct);

    private Task<bool> TaskExistsAsync(Guid taskId, CancellationToken ct) =>
        db.SqlTasks.AsNoTracking().AnyAsync(
            task => task.Id == taskId && task.PublicationStatus == PublicationStatus.Published, ct);
}
