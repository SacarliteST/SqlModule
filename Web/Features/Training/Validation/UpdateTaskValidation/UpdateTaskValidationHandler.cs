using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Validation;
using SQLModule.Data.Core;
using SQLModule.Domain.Training.Validation;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.Validation.UpdateTaskValidation;

internal sealed record UpdateTaskValidationCommand(
    Guid TaskId,
    Guid Version,
    TaskValidationDefinition Definition)
    : IRequest<Result<TaskValidationConfigurationResponse>>;

internal sealed class UpdateTaskValidationHandler(
    AppDbContext db,
    ITaskValidationEvaluationService evaluationService,
    ILogger<UpdateTaskValidationHandler> logger)
    : IRequestHandler<UpdateTaskValidationCommand, Result<TaskValidationConfigurationResponse>>
{
    public async Task<Result<TaskValidationConfigurationResponse>> Handle(
        UpdateTaskValidationCommand command,
        CancellationToken ct)
    {
        var taskInfo = await db.SqlTasks
            .AsNoTracking()
            .Where(task => task.Id == command.TaskId)
            .Select(task => new { task.ActiveValidationVersionId, task.SqlQuery.TargetDbId })
            .SingleOrDefaultAsync(ct);
        if (taskInfo is null)
        {
            return Result<TaskValidationConfigurationResponse>.Fail(
                TaskValidationErrors.TaskNotFound(command.TaskId));
        }

        var configuration = await db.TaskValidationConfigurations
            .Include(value => value.Checks)
            .SingleOrDefaultAsync(value => value.TaskId == command.TaskId, ct);
        if (configuration is null)
        {
            return Result<TaskValidationConfigurationResponse>.Fail(
                TaskValidationErrors.ConfigurationNotFound(command.TaskId));
        }

        if (configuration.Version != command.Version)
        {
            return Result<TaskValidationConfigurationResponse>.Fail(TaskValidationErrors.StaleVersion());
        }

        var existingCheckIds = configuration.Checks.Select(check => check.Id).ToHashSet();
        var unknownCheckId = command.Definition.Checks
            .Where(check => check.Id.HasValue)
            .Select(check => check.Id!.Value)
            .FirstOrDefault(checkId => !existingCheckIds.Contains(checkId));
        if (unknownCheckId != Guid.Empty)
        {
            return Result<TaskValidationConfigurationResponse>.Fail(
                TaskValidationErrors.CheckNotFound(unknownCheckId));
        }

        var evaluation = await evaluationService.EvaluateAsync(
            command.TaskId,
            command.Definition,
            TaskValidationMappings.ToIdentities(command.Definition),
            ct);
        if (!evaluation.IsSuccess)
        {
            return Result<TaskValidationConfigurationResponse>.Fail(evaluation.Error!);
        }

        if (!evaluation.Value!.Response.IsValid)
        {
            return Result<TaskValidationConfigurationResponse>.Fail(
                TaskValidationMappings.ToError(evaluation.Value.Response));
        }

        configuration.UpdateSettings(
            command.Definition.PassingScore,
            command.Definition.MaxAttempts,
            command.Definition.VisibleHintGroups);
        configuration.SynchronizeChecks(command.Definition.Checks);
        foreach (var addedCheck in configuration.Checks.Where(check => !existingCheckIds.Contains(check.Id)))
        {
            db.ValidationChecks.Add(addedCheck);
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogWarning(
                "Конфигурация проверки SQL-задания {TaskId} не обновлена: версия успела измениться",
                command.TaskId);
            return Result<TaskValidationConfigurationResponse>.Fail(TaskValidationErrors.StaleVersion());
        }

        var activeVersion = taskInfo.ActiveValidationVersionId.HasValue
            ? await db.TaskValidationVersions.AsNoTracking().SingleAsync(
                version => version.Id == taskInfo.ActiveValidationVersionId.Value,
                ct)
            : null;
        var tableNames = await db.MetaTables
            .AsNoTracking()
            .Where(table => table.TargetDbId == taskInfo.TargetDbId)
            .ToDictionaryAsync(table => table.Id, table => table.TableName, ct);

        logger.LogInformation(
            "Конфигурация проверки SQL-задания {TaskId} обновлена, версия {Version}",
            command.TaskId,
            configuration.Version);
        return TaskValidationMappings.ToResponse(configuration, activeVersion, tableNames);
    }
}
