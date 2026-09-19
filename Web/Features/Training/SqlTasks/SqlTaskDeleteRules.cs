using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Data.Core;
using SQLModule.Domain.Training;

namespace SQLModule.Web.Features.Training.SqlTasks;

/// <summary>
/// Единое правило «задание не использовалось»: им пользуются и команда удаления, и read-модель
/// (<c>canDelete</c>, <c>deleteBlockReasons</c>) — наборы правил не расходятся.
/// </summary>
internal interface ISqlTaskDeleteRules
{
    /// <summary>
    /// Возвращает все причины, мешающие удалению, в стабильном порядке. Пустой список — удалять можно.
    /// Задание должно существовать.
    /// </summary>
    Task<IReadOnlyList<Error>> EvaluateAsync(Guid taskId, CancellationToken ct);
}

internal sealed class SqlTaskDeleteRules(AppDbContext db) : ISqlTaskDeleteRules
{
    public async Task<IReadOnlyList<Error>> EvaluateAsync(Guid taskId, CancellationToken ct)
    {
        var status = await db.SqlTasks.AsNoTracking()
            .Where(value => value.Id == taskId)
            .Select(value => value.PublicationStatus)
            .SingleAsync(ct);

        var blockers = new List<Error>();
        if (status == PublicationStatus.Published)
        {
            blockers.Add(SqlTaskErrors.PublishedCannotBeDeleted);
        }

        if (await db.Attempts.AnyAsync(value => value.TaskId == taskId, ct))
        {
            blockers.Add(SqlTaskErrors.HasAttempts);
        }

        // Любое прохождение, включая платформенное и без попыток: оно ссылается на задание и версию оценки.
        if (await db.StudentTaskProgresses.AnyAsync(value => value.TaskId == taskId, ct))
        {
            blockers.Add(SqlTaskErrors.HasStudentProgress);
        }

        return blockers;
    }
}
