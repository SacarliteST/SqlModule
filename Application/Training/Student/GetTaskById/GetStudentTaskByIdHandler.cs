using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SQLModule.Application.Cqrs;
using SQLModule.Application.ModuleIntegration;
using SQLModule.Application.Persistence;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Student;
using SQLModule.Contracts.Training.Validation;
using SQLModule.Domain.ModuleIntegration;
using SQLModule.Domain.Training;
using SQLModule.Sandbox;

namespace SQLModule.Application.Training.Student;

/// <summary>Возвращает безопасные детали задания с учётом режима доступа.</summary>
public sealed class GetStudentTaskByIdHandler
    : IRequestHandler<GetStudentTaskByIdQuery, Result<StudentTaskDetailsResponse>>
{
    private readonly IApplicationDbContext db;
    private readonly IStudentTaskAccess taskAccess;
    private readonly SandboxOptions options;
    private readonly TimeProvider timeProvider;

    /// <summary>Создаёт обработчик деталей задания.</summary>
    public GetStudentTaskByIdHandler(
        IApplicationDbContext db,
        IStudentTaskAccess taskAccess,
        IOptions<SandboxOptions> options,
        TimeProvider timeProvider)
    {
        this.db = db;
        this.taskAccess = taskAccess;
        this.options = options.Value;
        this.timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<Result<StudentTaskDetailsResponse>> Handle(GetStudentTaskByIdQuery query, CancellationToken ct)
    {
        var access = await taskAccess.EnsureAsync(
            query.UserId, query.ModuleSessionId, query.TaskId, PlatformScopeMode.Read, ct);
        if (!access.IsSuccess)
        {
            return Result<StudentTaskDetailsResponse>.Fail(access.Error!);
        }

        var item = await db.SqlTasks.AsNoTracking()
            .Where(task => task.Id == query.TaskId && task.PublicationStatus == PublicationStatus.Published &&
                           (query.ModuleSessionId.HasValue || task.Visibility == TaskVisibility.Open))
            .Select(task => new
            {
                task.Id,
                task.TopicId,
                task.Topic.TopicName,
                task.TaskName,
                task.TaskText,
                task.DifficultyLevel,
                task.BankNumber,
                task.ActiveValidationVersionId,
                DbmsName = db.TargetDbs.Where(target => target.Id == task.SqlQuery.TargetDbId)
                    .Select(target => target.Dbms.DbmsName).First()
            })
            .FirstOrDefaultAsync(ct);
        if (item is null)
        {
            return Result<StudentTaskDetailsResponse>.Fail(StudentErrors.NotFound);
        }

        StudentTaskValidationResponse? validation = null;
        if (item.ActiveValidationVersionId.HasValue)
        {
            var moduleSession = query.ModuleSessionId.HasValue
                ? await db.ModuleSessions.AsNoTracking().SingleOrDefaultAsync(value =>
                    value.Id == query.ModuleSessionId.Value && value.UserId == query.UserId &&
                    value.TaskRef == query.TaskId.ToString(), ct)
                : null;
            var versionId = moduleSession?.ValidationVersionId ?? item.ActiveValidationVersionId;
            var version = versionId.HasValue
                ? await db.TaskValidationVersions.AsNoTracking()
                    .SingleOrDefaultAsync(value => value.Id == versionId.Value, ct)
                : null;
            if (version is not null)
            {
                StudentSessionStateResponse? sessionState = null;
                if (moduleSession is not null)
                {
                    var attemptsUsed = await db.Attempts.CountAsync(value =>
                        value.ModuleSessionId == moduleSession.Id && value.CountsTowardLimit, ct);
                    var bestScore = await db.Attempts
                        .Where(value => value.ModuleSessionId == moduleSession.Id && value.CountsTowardLimit)
                        .MaxAsync(value => (int?)value.Score, ct) ?? 0;
                    var maxAttempts = moduleSession.MaxAttempts;
                    int? remaining = maxAttempts.HasValue
                        ? Math.Max(0, maxAttempts.Value - attemptsUsed)
                        : null;
                    var active = moduleSession.Status == ModuleSessionStatus.Active &&
                                 !moduleSession.IsExpired(timeProvider.GetUtcNow());
                    sessionState = new StudentSessionStateResponse(
                        moduleSession.Status.ToContract(), bestScore, attemptsUsed, maxAttempts,
                        remaining, active && remaining != 0, active,
                        bestScore >= version.PassingScore, moduleSession.ExpiresAt,
                        moduleSession.FinalScore, moduleSession.FinalizationReason?.ToContract(),
                        moduleSession.FinalizedAt);
                }

                validation = new StudentTaskValidationResponse(
                    version.PassingScore,
                    sessionState,
                    query.ModuleSessionId.HasValue ? StudentHints.Build(version) : null);
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
            new StudentExecutionLimitsResponse(
                options.DefaultQueryTimeoutSeconds,
                options.MaxRows,
                options.MaxSqlLength),
            validation,
            query.ModuleSessionId.HasValue ? null : item.BankNumber);
    }
}
