using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.SqlTask;
using SQLModule.Data.Core;
using SQLModule.Domain.Training;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.SqlTasks;

internal sealed record PublishSqlTaskCommand(Guid Id)
    : IRequest<Result<SqlTaskResponse>>;

internal sealed class PublishSqlTaskHandler(AppDbContext db)
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

        if (await db.Attempts.AnyAsync(x => x.TaskId == command.Id, ct))
        {
            return Result<SqlTaskResponse>.Fail(SqlTaskErrors.HasAttemptsOnPublish(command.Id));
        }

        var reference = await db.SqlQueries
            .AsNoTracking()
            .Where(x => x.Id == task.SqlQueryId)
            .Select(x => new
            {
                x.ExpectedResult,
                TrainingDatabaseExists = db.TargetDbs.Any(targetDb => targetDb.Id == x.TargetDbId)
            })
            .FirstOrDefaultAsync(ct);

        if (reference is null || string.IsNullOrWhiteSpace(reference.ExpectedResult))
        {
            return Result<SqlTaskResponse>.Fail(SqlTaskErrors.ReferenceQueryNotValidated(command.Id));
        }

        if (!reference.TrainingDatabaseExists)
        {
            return Result<SqlTaskResponse>.Fail(SqlTaskErrors.TrainingDatabaseUnavailable(command.Id));
        }

        task.Publish();
        await db.SaveChangesAsync(ct);
        return SqlTaskMappings.ToResponse(task);
    }
}
