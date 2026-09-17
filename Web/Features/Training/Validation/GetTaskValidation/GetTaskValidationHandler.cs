using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Validation;
using SQLModule.Data.Core;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.Validation.GetTaskValidation;

internal sealed record GetTaskValidationQuery(Guid TaskId)
    : IRequest<Result<TaskValidationConfigurationResponse>>;

internal sealed class GetTaskValidationHandler(
    AppDbContext db,
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
            .SingleOrDefaultAsync(value => value.TaskId == query.TaskId, ct);
        if (configuration is null)
        {
            configuration = TaskValidationDefaults.Create(query.TaskId);
            db.TaskValidationConfigurations.Add(configuration);
            await db.SaveChangesAsync(ct);
            logger.LogInformation(
                "Для SQL-задания {TaskId} создана конфигурация проверки по умолчанию",
                query.TaskId);
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

        return TaskValidationMappings.ToResponse(configuration, activeVersion, tableNames);
    }
}
