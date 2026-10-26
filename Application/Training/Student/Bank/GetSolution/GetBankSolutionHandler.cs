using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Persistence;
using SQLModule.Application.Training.Bank;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Student;
using SQLModule.Domain.Training;

namespace SQLModule.Application.Training.Student;

/// <summary>Выдаёт эталон и пояснение после успешной тренировочной попытки.</summary>
public sealed class GetBankSolutionHandler : IRequestHandler<GetBankSolutionQuery, Result<BankSolutionResponse>>
{
    private readonly IApplicationDbContext db;
    private readonly TimeProvider timeProvider;

    /// <summary>Создаёт обработчик разбора решения.</summary>
    public GetBankSolutionHandler(IApplicationDbContext db, TimeProvider timeProvider)
    {
        this.db = db;
        this.timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<Result<BankSolutionResponse>> Handle(GetBankSolutionQuery query, CancellationToken ct)
    {
        var task = await db.SqlTasks.AsNoTracking()
            .Where(value => value.Id == query.TaskId &&
                            value.PublicationStatus == PublicationStatus.Published &&
                            value.Visibility == TaskVisibility.Open &&
                            value.BankPublishedAt != null)
            .Select(value => new
            {
                value.ShowSolution,
                value.SolutionExplanation,
                value.BankPublishedAt,
                SqlText = value.SqlQuery.QueryText
            })
            .SingleOrDefaultAsync(ct);
        if (task is null)
        {
            return Result<BankSolutionResponse>.Fail(StudentErrors.NotFound);
        }

        if (!task.ShowSolution)
        {
            return Result<BankSolutionResponse>.Fail(TaskBankErrors.SolutionDisabled);
        }

        var solved = await (
            from attempt in db.Attempts.AsNoTracking()
            join version in db.TaskValidationVersions.AsNoTracking()
                on attempt.ValidationVersionId equals version.Id
            where attempt.UserId == query.UserId &&
                  attempt.TaskId == query.TaskId &&
                  attempt.ModuleSessionId == null &&
                  attempt.FinishedAt >= task.BankPublishedAt!.Value &&
                  attempt.Score >= version.PassingScore
            select attempt.Id).AnyAsync(ct);
        if (!solved)
        {
            return Result<BankSolutionResponse>.Fail(TaskBankErrors.SolutionAttemptRequired);
        }

        db.BankSolutionAccesses.Add(BankSolutionAccess.Create(
            query.UserId,
            query.TaskId,
            timeProvider.GetUtcNow()));
        await db.SaveChangesAsync(ct);
        return new BankSolutionResponse(task.SqlText, task.SolutionExplanation);
    }
}
