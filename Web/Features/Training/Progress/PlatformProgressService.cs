using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SQLModule.Common.Results;
using SQLModule.Data.Core;
using SQLModule.Domain.ModuleIntegration;
using SQLModule.Domain.Training;

namespace SQLModule.Web.Features.Training.Progress;

internal interface IPlatformProgressService
{
    Task<Result<StudentTaskProgress>> EnsureCreatedAsync(ModuleSession session, CancellationToken ct);
}

internal sealed class PlatformProgressService(
    AppDbContext db,
    ILogger<PlatformProgressService> logger) : IPlatformProgressService
{
    public async Task<Result<StudentTaskProgress>> EnsureCreatedAsync(
        ModuleSession session,
        CancellationToken ct)
    {
        if (!Guid.TryParse(session.TaskRef, out var taskId))
        {
            return Result<StudentTaskProgress>.Fail(Error.Validation(
                "ModuleSession.InvalidTaskRef",
                "Платформенная сессия содержит некорректный идентификатор задания."));
        }

        var existing = await db.StudentTaskProgresses
            .SingleOrDefaultAsync(value => value.ModuleSessionId == session.Id, ct);
        if (existing is not null)
        {
            if (existing.UserId != session.UserId || existing.TaskId != taskId)
            {
                return Result<StudentTaskProgress>.Fail(Error.Conflict(
                    "ModuleSession.ProgressMismatch",
                    "Платформенная сессия уже связана с другим прохождением."));
            }

            existing.SynchronizePlatformExpiry(session.ExpiresAt);
            return existing;
        }

        var activeVersionId = await db.SqlTasks.AsNoTracking()
            .Where(task => task.Id == taskId && task.PublicationStatus == PublicationStatus.Published)
            .Select(task => task.ActiveValidationVersionId)
            .SingleOrDefaultAsync(ct);
        if (!activeVersionId.HasValue)
        {
            return Result<StudentTaskProgress>.Fail(Error.Validation(
                "ModuleSession.TaskValidationUnavailable",
                "Для задания платформенной сессии не опубликована конфигурация проверки."));
        }

        var progress = StudentTaskProgress.CreatePlatform(
            session.UserId,
            taskId,
            activeVersionId.Value,
            session.Id,
            session.ExpiresAt);
        db.StudentTaskProgresses.Add(progress);
        logger.LogInformation(
            "Для платформенной сессии {SessionId} подготовлено прохождение {ProgressId} задания {TaskId}",
            session.Id, progress.Id, taskId);
        return progress;
    }
}
