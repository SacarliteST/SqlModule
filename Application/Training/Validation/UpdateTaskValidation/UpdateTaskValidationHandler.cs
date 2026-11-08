using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Persistence;
using SQLModule.Application.Training.Bank;
using SQLModule.Application.Training.Validation;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Validation;
using SQLModule.Domain.Training;
using SQLModule.Domain.Training.Validation;

namespace SQLModule.Application.Training.Validation.UpdateTaskValidation;

public sealed class UpdateTaskValidationHandler
    : IRequestHandler<UpdateTaskValidationCommand, Result<TaskValidationConfigurationResponse>>
{
    private readonly IApplicationDbContext db;
    private readonly ITaskValidationEvaluationService evaluationService;
    private readonly ValidationSkillResolver skillResolver;
    private readonly ILogger<UpdateTaskValidationHandler> logger;

    /// <summary>Создаёт обработчик обновления validation-конфигурации.</summary>
    public UpdateTaskValidationHandler(
        IApplicationDbContext db,
        ITaskValidationEvaluationService evaluationService,
        ValidationSkillResolver skillResolver,
        ILogger<UpdateTaskValidationHandler> logger)
    {
        this.db = db;
        this.evaluationService = evaluationService;
        this.skillResolver = skillResolver;
        this.logger = logger;
    }

    public async Task<Result<TaskValidationConfigurationResponse>> Handle(
        UpdateTaskValidationCommand command,
        CancellationToken ct)
    {
        var taskInfo = await db.SqlTasks
            .AsNoTracking()
            .Where(task => task.Id == command.TaskId)
            .Select(task => new
            {
                task.ActiveValidationVersionId,
                task.SqlQuery.TargetDbId,
                task.Visibility,
                task.BankHint
            })
            .SingleOrDefaultAsync(ct);
        if (taskInfo is null)
        {
            return Result<TaskValidationConfigurationResponse>.Fail(
                TaskValidationErrors.TaskNotFound(command.TaskId));
        }

        var configuration = await db.TaskValidationConfigurations
            .Include(value => value.Checks)
            .Include(value => value.SkillTags)
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

        if (taskInfo.Visibility == TaskVisibility.Open &&
            String.IsNullOrWhiteSpace(taskInfo.BankHint) &&
            !TaskBankHintRules.HasAutomaticHint(
                command.Definition.VisibleHintGroups,
                command.Definition.Checks.Select(check => check.Kind)))
        {
            return Result<TaskValidationConfigurationResponse>.Fail(TaskBankErrors.HintMissing);
        }

        var assignedSkills = await skillResolver.EnsureAsync(command.Definition, ct);
        if (!assignedSkills.IsSuccess)
        {
            return Result<TaskValidationConfigurationResponse>.Fail(assignedSkills.Error!);
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
            command.Definition.VisibleHintGroups);
        configuration.SynchronizeChecks(command.Definition.Checks);
        var existingTagIds = configuration.SkillTags.Select(tag => tag.Id).ToHashSet();
        configuration.SynchronizeSkillTags(command.Definition.EffectiveSkillTagIds);
        foreach (var addedCheck in configuration.Checks.Where(check => !existingCheckIds.Contains(check.Id)))
        {
            db.ValidationChecks.Add(addedCheck);
        }
        foreach (var addedTag in configuration.SkillTags.Where(tag => !existingTagIds.Contains(tag.Id)))
        {
            db.TaskValidationSkillTags.Add(addedTag);
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
        return TaskValidationMappings.ToResponse(
            configuration, activeVersion, tableNames, assignedSkills.Value!);
    }
}
