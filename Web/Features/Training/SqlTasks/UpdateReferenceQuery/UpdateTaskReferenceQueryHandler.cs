using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.SqlTask;
using SQLModule.Data.Core;
using SQLModule.Sandbox;
using SQLModule.Web.Common.Cqrs;
using SQLModule.Web.Common.Sandbox;
using SQLModule.Web.Features.Training.SqlQueries;

namespace SQLModule.Web.Features.Training.SqlTasks;

internal sealed record UpdateTaskReferenceQueryCommand(
    Guid TaskId,
    Guid TargetDbId,
    string QueryText,
    bool StrictColumnOrder,
    bool StrictRowOrder) : IRequest<Result<UpdateTaskReferenceQueryResponse>>;

internal sealed class UpdateTaskReferenceQueryHandler(
    ISqlQueryValidationRunner validationRunner,
    AppDbContext db)
    : IRequestHandler<UpdateTaskReferenceQueryCommand, Result<UpdateTaskReferenceQueryResponse>>
{
    public async Task<Result<UpdateTaskReferenceQueryResponse>> Handle(
        UpdateTaskReferenceQueryCommand command,
        CancellationToken ct)
    {
        var task = await db.SqlTasks
            .Include(x => x.SqlQuery)
            .FirstOrDefaultAsync(x => x.Id == command.TaskId, ct);

        if (task is null)
        {
            return Result<UpdateTaskReferenceQueryResponse>.Fail(SqlTaskErrors.NotFound(command.TaskId));
        }

        var hasAttempts = await db.Attempts.AnyAsync(x => x.TaskId == command.TaskId, ct);
        var restriction = ReferenceQueryEditPolicy.GetRestriction(task.PublicationStatus, hasAttempts);
        if (restriction is ReferenceQueryEditRestriction.Published or ReferenceQueryEditRestriction.Archived)
        {
            return Result<UpdateTaskReferenceQueryResponse>.Fail(
                SqlTaskErrors.ReferenceChangeRequiresDraft(command.TaskId));
        }

        if (restriction == ReferenceQueryEditRestriction.HasAttempts)
        {
            return Result<UpdateTaskReferenceQueryResponse>.Fail(
                SqlTaskErrors.ReferenceChangeBlockedByAttempts(command.TaskId));
        }

        var run = await validationRunner.ValidateAsync(command.TargetDbId, command.QueryText, ct);
        if (!run.IsSuccess)
        {
            return Result<UpdateTaskReferenceQueryResponse>.Fail(run.Error!);
        }

        task.SqlQuery.Update(
            command.TargetDbId,
            command.QueryText,
            command.StrictColumnOrder,
            command.StrictRowOrder);
        task.SqlQuery.SetExpectedResult(GoldenResult.Serialize(run.Value!));

        await db.SaveChangesAsync(ct);

        var targetDb = await db.TargetDbs
            .AsNoTracking()
            .Where(x => x.Id == task.SqlQuery.TargetDbId)
            .Select(x => new UpdateTaskReferenceTargetDbResponse(
                x.Id,
                x.DbName,
                x.Dbms.DbmsName))
            .SingleAsync(ct);

        return new UpdateTaskReferenceQueryResponse(
            task.SqlQuery.QueryText,
            task.SqlQuery.StrictColumnOrder,
            task.SqlQuery.StrictRowOrder,
            targetDb);
    }
}
