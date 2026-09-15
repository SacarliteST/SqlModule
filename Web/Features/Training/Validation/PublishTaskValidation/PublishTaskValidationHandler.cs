using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Validation;
using SQLModule.Data.Core;
using SQLModule.Domain;
using SQLModule.Domain.Common;
using SQLModule.Domain.Training;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.Validation.PublishTaskValidation;

internal sealed record PublishTaskValidationCommand(Guid TaskId, Guid Version)
    : IRequest<Result<TaskValidationConfigurationResponse>>;

internal sealed class PublishTaskValidationHandler(
    AppDbContext db,
    ITaskValidationEvaluationService evaluationService,
    ITaskValidationSnapshotFactory snapshotFactory,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    ILogger<PublishTaskValidationHandler> logger)
    : IRequestHandler<PublishTaskValidationCommand, Result<TaskValidationConfigurationResponse>>
{
    public async Task<Result<TaskValidationConfigurationResponse>> Handle(
        PublishTaskValidationCommand command,
        CancellationToken ct)
    {
        var task = await db.SqlTasks
            .Include(value => value.ValidationConfiguration)
                .ThenInclude(configuration => configuration!.Checks)
            .Include(value => value.ActiveValidationVersion)
            .Include(value => value.SqlQuery)
            .SingleOrDefaultAsync(value => value.Id == command.TaskId, ct);
        if (task is null)
        {
            return Result<TaskValidationConfigurationResponse>.Fail(
                TaskValidationErrors.TaskNotFound(command.TaskId));
        }

        var configuration = task.ValidationConfiguration;
        if (configuration is null)
        {
            return Result<TaskValidationConfigurationResponse>.Fail(
                TaskValidationErrors.ConfigurationNotFound(command.TaskId));
        }

        if (configuration.Version != command.Version)
        {
            return Result<TaskValidationConfigurationResponse>.Fail(TaskValidationErrors.StaleVersion());
        }

        if (task.ActiveValidationVersion?.ConfigurationVersion == configuration.Version)
        {
            return await ToResponseAsync(task, configuration, task.ActiveValidationVersion, ct);
        }

        var definition = TaskValidationMappings.ToDefinition(configuration);
        var evaluation = await evaluationService.EvaluateAsync(
            command.TaskId,
            definition,
            TaskValidationMappings.ToIdentities(definition),
            ct);
        if (!evaluation.IsSuccess)
        {
            return Result<TaskValidationConfigurationResponse>.Fail(evaluation.Error!);
        }

        if (!evaluation.Value!.Response.IsValid || evaluation.Value.ReferenceResult is null)
        {
            return Result<TaskValidationConfigurationResponse>.Fail(
                TaskValidationMappings.ToError(evaluation.Value.Response));
        }

        var snapshot = await snapshotFactory.CreateAsync(
            command.TaskId,
            configuration,
            evaluation.Value.ReferenceResult,
            evaluation.Value.Response.AnalyzerVersion,
            ct);
        if (!snapshot.IsSuccess)
        {
            return Result<TaskValidationConfigurationResponse>.Fail(snapshot.Error!);
        }

        var currentVersion = await db.TaskValidationConfigurations
            .AsNoTracking()
            .Where(value => value.Id == configuration.Id)
            .Select(value => value.Version)
            .SingleAsync(ct);
        if (currentVersion != command.Version)
        {
            return Result<TaskValidationConfigurationResponse>.Fail(TaskValidationErrors.StaleVersion());
        }

        var nextVersionNumber = await db.TaskValidationVersions
            .Where(version => version.TaskId == command.TaskId)
            .Select(version => (int?)version.VersionNumber)
            .MaxAsync(ct) + 1 ?? 1;
        var userId = currentUser.UserId ?? SystemUser.Id;
        var userName = currentUser.DisplayName ?? userId.ToString();
        var version = TaskValidationVersion.Publish(
            command.TaskId,
            nextVersionNumber,
            configuration.Version,
            configuration.PassingScore,
            configuration.MaxAttempts,
            configuration.VisibleHintGroupsMask,
            snapshot.Value!,
            userId,
            userName,
            timeProvider.GetUtcNow());
        db.TaskValidationVersions.Add(version);
        task.ActivateValidationVersion(version.Id);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException exception)
        {
            var concurrentlyPublished = await db.TaskValidationVersions
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    value => value.TaskId == command.TaskId &&
                             value.ConfigurationVersion == command.Version,
                    ct);
            if (concurrentlyPublished is not null)
            {
                logger.LogInformation(
                    "Версия проверки SQL-задания {TaskId} уже опубликована параллельным запросом",
                    command.TaskId);
                return await ToResponseAsync(task, configuration, concurrentlyPublished, ct);
            }

            logger.LogWarning(
                exception,
                "Версия проверки SQL-задания {TaskId} не опубликована из-за конкурентного изменения",
                command.TaskId);
            return Result<TaskValidationConfigurationResponse>.Fail(TaskValidationErrors.StaleVersion());
        }

        logger.LogInformation(
            "Опубликована версия {VersionNumber} проверки SQL-задания {TaskId}",
            version.VersionNumber,
            command.TaskId);
        return await ToResponseAsync(task, configuration, version, ct);
    }

    private async Task<TaskValidationConfigurationResponse> ToResponseAsync(
        SqlTask task,
        TaskValidationConfiguration configuration,
        TaskValidationVersion version,
        CancellationToken ct)
    {
        var tableNames = await db.MetaTables
            .AsNoTracking()
            .Where(table => table.TargetDbId == task.SqlQuery.TargetDbId)
            .ToDictionaryAsync(table => table.Id, table => table.TableName, ct);
        return TaskValidationMappings.ToResponse(configuration, version, tableNames);
    }
}
