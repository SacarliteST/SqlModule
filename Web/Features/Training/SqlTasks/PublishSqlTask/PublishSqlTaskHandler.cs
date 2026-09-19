using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.SqlTask;
using SQLModule.Data.Core;
using SQLModule.Domain.Training;
using SQLModule.Web.Common.Cqrs;
using SQLModule.Web.Features.Training.SqlQueries;

namespace SQLModule.Web.Features.Training.SqlTasks;

internal sealed record PublishSqlTaskCommand(Guid Id)
    : IRequest<Result<SqlTaskResponse>>;

internal sealed class PublishSqlTaskHandler(
    ISqlQueryValidationRunner validationRunner,
    ISqlTaskPublishReadiness readiness,
    AppDbContext db)
    : IRequestHandler<PublishSqlTaskCommand, Result<SqlTaskResponse>>
{
    public async Task<Result<SqlTaskResponse>> Handle(PublishSqlTaskCommand command, CancellationToken ct)
    {
        var task = await db.SqlTasks.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (task is null)
        {
            return Result<SqlTaskResponse>.Fail(SqlTaskErrors.NotFound(command.Id));
        }

        if (task.PublicationStatus == PublicationStatus.Published)
        {
            return Result<SqlTaskResponse>.Fail(SqlTaskErrors.AlreadyPublished(command.Id));
        }

        if (task.PublicationStatus == PublicationStatus.Archived)
        {
            return Result<SqlTaskResponse>.Fail(SqlTaskErrors.ArchivedCannotBePublished(command.Id));
        }

        var blockers = await readiness.EvaluateAsync(command.Id, ct);
        if (blockers.Count > 0)
        {
            // Первая причина определяет code и статус ответа, остальные — в errors.
            return Result<SqlTaskResponse>.Fail(blockers.Count == 1
                ? blockers[0]
                : blockers[0] with
                {
                    Errors = blockers.ToDictionary(blocker => blocker.Code, blocker => new[] { blocker.Message })
                });
        }

        var reference = await db.SqlQueries
            .AsNoTracking()
            .Where(x => x.Id == task.SqlQueryId)
            .Select(x => new { x.TargetDbId, x.QueryText })
            .FirstAsync(ct);

        var validation = await validationRunner.ValidateAsync(
            reference.TargetDbId, reference.QueryText, ct);
        if (!validation.IsSuccess)
        {
            return Result<SqlTaskResponse>.Fail(validation.Error!);
        }

        task.Publish();
        await db.SaveChangesAsync(ct);
        return SqlTaskMappings.ToResponse(task);
    }
}
