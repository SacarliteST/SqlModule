using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Attempt;
using SQLModule.Data.Core;
using SQLModule.Domain.Common;
using SQLModule.Domain.Training;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Sandbox;
using SQLModule.Sandbox;

namespace SQLModule.Host.Features.Training.Attempts.SubmitAttempt;

internal record SubmitAttemptCommand(Guid UserId, Guid TaskId, string SubmittedSql)
    : IRequest<Result<SubmitAttemptResponse>>;

internal sealed class SubmitAttemptHandler(
    ITaskMaterializer materializer,
    ISandboxExecutor executor,
    IResultComparer comparer,
    IOptions<SandboxOptions> sandboxOptions,
    TimeProvider timeProvider,
    AppDbContext db)
    : IRequestHandler<SubmitAttemptCommand, Result<SubmitAttemptResponse>>
{
    public async Task<Result<SubmitAttemptResponse>> Handle(
        SubmitAttemptCommand command, CancellationToken ct)
    {
        var task = await db.SqlTasks.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == command.TaskId, ct);

        if (task is null)
        {
            return Result<SubmitAttemptResponse>.Fail(AttemptErrors.TaskNotFound(command.TaskId));
        }

        var sqlQuery = await db.SqlQueries.AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == task.SqlQueryId, ct);

        if (sqlQuery?.ExpectedResult is null)
        {
            return Result<SubmitAttemptResponse>.Fail(AttemptErrors.ReferenceNotReady);
        }

        var golden = GoldenResult.Deserialize(sqlQuery.ExpectedResult);
        if (golden is null)
        {
            return Result<SubmitAttemptResponse>.Fail(AttemptErrors.ReferenceNotReady);
        }

        var mat = await materializer.MaterializeAsync(sqlQuery.TargetDbId, ct);
        if (!mat.IsSuccess)
        {
            return Result<SubmitAttemptResponse>.Fail(mat.Error!);
        }

        var opts = sandboxOptions.Value;
        var startedAt = timeProvider.GetUtcNow();

        var run = await executor.RunAsync(
            mat.Value!.Dbms.ToSandboxSpec(),
            mat.Value.Setup,
            new SandboxQuery(command.SubmittedSql, opts.DefaultQueryTimeoutSeconds, opts.MaxRows),
            ct);

        var finishedAt = timeProvider.GetUtcNow();

        if (!run.IsSuccess)
        {
            return Result<SubmitAttemptResponse>.Fail(run.Error!);
        }

        var result = run.Value!;

        ExecutionStatus status;
        CheckReason reason;
        bool isCorrect;
        IReadOnlyList<string> actualColumns;
        IReadOnlyList<IReadOnlyList<string?>> actualRows;

        if (!result.Succeeded)
        {
            var isTimeout = result.Error is not null &&
                            result.Error.Contains("timeout", StringComparison.OrdinalIgnoreCase);
            status = isTimeout ? ExecutionStatus.TimedOut : ExecutionStatus.Error;
            reason = isTimeout ? CheckReason.Timeout : CheckReason.SqlError;
            isCorrect = false;
            actualColumns = [];
            actualRows = [];
        }
        else
        {
            status = ExecutionStatus.Succeeded;
            var outcome = comparer.Compare(golden, result);
            isCorrect = outcome.IsCorrect;
            reason = outcome.Reason;
            actualColumns = result.Columns;
            actualRows = result.Rows;
        }

        var userId = command.UserId == Guid.Empty ? SystemUser.Id : command.UserId;

        var attempt = Attempt.Record(
            userId, command.TaskId, command.SubmittedSql,
            status, isCorrect, reason,
            result.Succeeded ? result.RowCount : null,
            result.Succeeded ? result.DurationMs : null,
            result.Succeeded ? null : result.Error,
            startedAt, finishedAt);

        db.Attempts.Add(attempt);
        await db.SaveChangesAsync(ct);

        return new SubmitAttemptResponse(
            attempt.Id, status, isCorrect, reason,
            result.Succeeded ? result.RowCount : null,
            result.Succeeded ? result.DurationMs : null,
            result.Succeeded ? null : result.Error,
            actualColumns, actualRows);
    }
}
