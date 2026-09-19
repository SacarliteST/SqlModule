using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SQLModule.Common.Results;
using SQLModule.Data.Core;
using SQLModule.Sandbox;
using SQLModule.Web.Common.Sandbox;
using SQLModule.Web.Features.Training.Validation;

namespace SQLModule.Web.Features.Training.SqlTasks;

/// <summary>
/// Единый анализ готовности задания к запуску студентом. Им пользуются и read-модель
/// (<c>canPublish</c>, <c>publishBlockers</c>), и команда публикации — наборы правил не расходятся.
/// </summary>
internal interface ISqlTaskPublishReadiness
{
    /// <summary>
    /// Возвращает все причины, мешающие публикации, в стабильном порядке. Пустой список — задание готово.
    /// Задание должно существовать.
    /// </summary>
    Task<IReadOnlyList<Error>> EvaluateAsync(Guid taskId, CancellationToken ct);
}

internal sealed class SqlTaskPublishReadiness(
    AppDbContext db,
    ITaskValidationSnapshotFactory snapshotFactory,
    IOptions<SandboxOptions> sandboxOptions) : ISqlTaskPublishReadiness
{
    public async Task<IReadOnlyList<Error>> EvaluateAsync(Guid taskId, CancellationToken ct)
    {
        var task = await db.SqlTasks.AsNoTracking()
            .Where(value => value.Id == taskId)
            .Select(value => new { value.SqlQueryId, value.ActiveValidationVersionId })
            .SingleAsync(ct);
        var version = task.ActiveValidationVersionId.HasValue
            ? await db.TaskValidationVersions.AsNoTracking()
                .Where(value => value.Id == task.ActiveValidationVersionId.Value)
                .Select(value => new
                {
                    value.SchemaSnapshotJson,
                    value.DatasetSnapshotJson,
                    value.ReferenceQuerySnapshotJson
                })
                .SingleOrDefaultAsync(ct)
            : null;
        var query = await db.SqlQueries.AsNoTracking()
            .Where(value => value.Id == task.SqlQueryId)
            .Select(value => new { value.QueryText, value.ExpectedResult, value.TargetDbId })
            .SingleOrDefaultAsync(ct);

        var referenceMissing = query is null || String.IsNullOrWhiteSpace(query.QueryText);
        var targetDbExists = query is not null &&
                             await db.TargetDbs.AnyAsync(value => value.Id == query.TargetDbId, ct);
        var notValidated = false;
        if (!referenceMissing)
        {
            notValidated = String.IsNullOrWhiteSpace(query!.ExpectedResult);
            if (!notValidated && version is not null && targetDbExists)
            {
                // Результат эталона устарел, если схема, данные или сам эталон изменились после публикации версии.
                notValidated = !await SnapshotFreshness.IsFreshAsync(
                    snapshotFactory,
                    taskId,
                    version.SchemaSnapshotJson,
                    version.DatasetSnapshotJson,
                    version.ReferenceQuerySnapshotJson,
                    ct);
            }
        }

        var blockers = new List<Error>();
        if (version is null)
        {
            blockers.Add(SqlTaskErrors.ValidationVersionNotPublished);
        }

        if (referenceMissing)
        {
            blockers.Add(SqlTaskErrors.ReferenceQueryMissing);
        }

        if (notValidated)
        {
            blockers.Add(SqlTaskErrors.ReferenceQueryNotValidated);
        }

        if (query is not null && !targetDbExists)
        {
            blockers.Add(SqlTaskErrors.TrainingDatabaseUnavailable);
        }

        if (await db.Attempts.AnyAsync(value => value.TaskId == taskId, ct))
        {
            blockers.Add(SqlTaskErrors.HasAttemptsOnPublish);
        }

        if (!referenceMissing && !notValidated && !String.IsNullOrWhiteSpace(query!.ExpectedResult))
        {
            var comparisonRowLimit = Math.Max(1, sandboxOptions.Value.ComparisonMaxRows);
            var golden = GoldenResult.Deserialize(query.ExpectedResult);
            if (golden?.Rows.Count > comparisonRowLimit)
            {
                blockers.Add(SqlTaskErrors.ReferenceResultExceedsComparisonLimit(comparisonRowLimit));
            }
        }

        return blockers;
    }
}
