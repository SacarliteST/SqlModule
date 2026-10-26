using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Persistence;
using SQLModule.Application.Training.Attempts;
using SQLModule.Application.Training.Bank;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Student;
using SQLModule.Domain.Training;

namespace SQLModule.Application.Training.Student;

/// <summary>Выдаёт подсказку по последней неуспешной тренировочной попытке.</summary>
public sealed class GetBankHintHandler : IRequestHandler<GetBankHintQuery, Result<BankHintResponse>>
{
    private readonly IApplicationDbContext db;
    private readonly TimeProvider timeProvider;

    /// <summary>Создаёт обработчик подсказки.</summary>
    public GetBankHintHandler(IApplicationDbContext db, TimeProvider timeProvider)
    {
        this.db = db;
        this.timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<Result<BankHintResponse>> Handle(GetBankHintQuery query, CancellationToken ct)
    {
        var task = await db.SqlTasks.AsNoTracking()
            .Where(value => value.Id == query.TaskId &&
                            value.PublicationStatus == PublicationStatus.Published &&
                            value.Visibility == TaskVisibility.Open &&
                            value.BankPublishedAt != null)
            .Select(value => new { value.BankPublishedAt, value.BankHint })
            .SingleOrDefaultAsync(ct);
        if (task is null)
        {
            return Result<BankHintResponse>.Fail(StudentErrors.NotFound);
        }

        var attempt = await (
            from candidate in db.Attempts.AsNoTracking()
            join version in db.TaskValidationVersions.AsNoTracking()
                on candidate.ValidationVersionId equals version.Id
            where candidate.UserId == query.UserId &&
                  candidate.TaskId == query.TaskId &&
                  candidate.ModuleSessionId == null &&
                  candidate.FinishedAt >= task.BankPublishedAt!.Value &&
                  candidate.Score < version.PassingScore
            orderby candidate.FinishedAt descending, candidate.Id descending
            select new
            {
                candidate.Id,
                AttemptNumber = candidate.AttemptNumber!.Value,
                Version = version
            }).FirstOrDefaultAsync(ct);
        if (attempt is null)
        {
            return Result<BankHintResponse>.Fail(TaskBankErrors.HintAttemptRequired);
        }

        var visible = attempt.Version.GetVisibleHintGroups().ToHashSet();
        var groups = await db.AttemptCheckResults.AsNoTracking()
            .Where(value => value.AttemptId == attempt.Id && value.Status == ValidationCheckStatus.Failed)
            .OrderBy(value => value.Order)
            .Select(value => value.Kind)
            .ToListAsync(ct);
        var issuedGroups = groups
            .Select(AttemptScoringReadService.ToHintGroup)
            .Where(group => group.HasValue && visible.Contains(group.Value))
            .Select(group => group!.Value)
            .Distinct()
            .ToArray();
        var autoHints = issuedGroups
            .Select(group => new BankAutoHintResponse(group.ToContract(), MessageFor(group)))
            .ToArray();
        var authorHint = String.IsNullOrWhiteSpace(task.BankHint) ? null : task.BankHint;
        db.BankHintAccesses.Add(BankHintAccess.Create(
            query.UserId,
            query.TaskId,
            attempt.Id,
            attempt.AttemptNumber,
            issuedGroups,
            authorHint is not null,
            timeProvider.GetUtcNow()));
        await db.SaveChangesAsync(ct);
        return new BankHintResponse(attempt.AttemptNumber, autoHints, authorHint);
    }

    private static string MessageFor(HintGroup group) => group switch
    {
        HintGroup.Result => "Результат запроса пока не совпадает с ожидаемым.",
        HintGroup.RequiredConstructs => "Проверьте наличие обязательных SQL-конструкций.",
        HintGroup.ForbiddenConstructs => "Проверьте, не использованы ли запрещённые SQL-конструкции.",
        HintGroup.RequiredTables => "Проверьте использование обязательных таблиц.",
        HintGroup.ForbiddenTables => "Проверьте, не использованы ли запрещённые таблицы.",
        _ => "Проверьте соответствующую часть решения."
    };
}
