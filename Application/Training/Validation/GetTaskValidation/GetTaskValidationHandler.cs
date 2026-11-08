using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Persistence;
using SQLModule.Application.Training.Validation;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Validation;

namespace SQLModule.Application.Training.Validation.GetTaskValidation;

public sealed record GetTaskValidationQuery(Guid TaskId)
    : IRequest<Result<TaskValidationConfigurationResponse>>;

public sealed class GetTaskValidationHandler(
    IApplicationDbContext db,
    ValidationSkillResolver skillResolver,
    ILogger<GetTaskValidationHandler> logger)
    : IRequestHandler<GetTaskValidationQuery, Result<TaskValidationConfigurationResponse>>
{
    public async Task<Result<TaskValidationConfigurationResponse>> Handle(
        GetTaskValidationQuery query,
        CancellationToken ct)
    {
        var taskInfo = await db.SqlTasks
            .AsNoTracking()
            .Where(task => task.Id == query.TaskId)
            .Select(task => new { task.ActiveValidationVersionId, task.SqlQuery.TargetDbId })
            .SingleOrDefaultAsync(ct);
        if (taskInfo is null)
        {
            return Result<TaskValidationConfigurationResponse>.Fail(
                TaskValidationErrors.TaskNotFound(query.TaskId));
        }

        var configuration = await db.TaskValidationConfigurations
            .AsNoTracking()
            .Include(value => value.Checks)
            .Include(value => value.SkillTags)
            .SingleOrDefaultAsync(value => value.TaskId == query.TaskId, ct);
        if (configuration is null)
        {
            configuration = TaskValidationDefaults.Create(query.TaskId);
            db.TaskValidationConfigurations.Add(configuration);
            logger.LogInformation(
                "Для SQL-задания {TaskId} создана конфигурация проверки по умолчанию",
                query.TaskId);
        }

        var assignedSkills = await skillResolver.EnsureAsync(
            TaskValidationMappings.ToDefinition(configuration), ct);
        if (!assignedSkills.IsSuccess)
        {
            return Result<TaskValidationConfigurationResponse>.Fail(assignedSkills.Error!);
        }
        await db.SaveChangesAsync(ct);

        var activeVersion = taskInfo.ActiveValidationVersionId.HasValue
            ? await db.TaskValidationVersions.AsNoTracking().SingleAsync(
                version => version.Id == taskInfo.ActiveValidationVersionId.Value,
                ct)
            : null;
        var tableNames = await db.MetaTables
            .AsNoTracking()
            .Where(table => table.TargetDbId == taskInfo.TargetDbId)
            .ToDictionaryAsync(table => table.Id, table => table.TableName, ct);

        return TaskValidationMappings.ToResponse(
            configuration, activeVersion, tableNames, assignedSkills.Value!);
    }
}
