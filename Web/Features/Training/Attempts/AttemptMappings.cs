using SQLModule.Contracts.Training.Attempt;
using SQLModule.Domain.Training;

namespace SQLModule.Web.Features.Training.Attempts;

internal static class AttemptMappings
{
    internal static AttemptResponse ToResponse(
        Attempt e,
        AttemptResultSnapshot snapshot,
        global::SQLModule.Contracts.Training.Validation.AttemptScoringResponse? scoring = null) => new(
        e.Id, e.UserId, e.StudentName, e.TaskId, e.SubmittedSql,
        e.Status, e.IsCorrect, e.Reason,
        e.RowCount, e.DurationMs, ToPublicError(e),
        e.StartedAt, e.FinishedAt,
        e.CreatedById == Guid.Empty ? e.UserId : e.CreatedById, e.StartedAt,
        e.UpdatedById == Guid.Empty ? e.UserId : e.UpdatedById, e.FinishedAt,
        PublicError: ToPublicError(e),
        CreatedByName: e.CreatedByName, UpdatedByName: e.UpdatedByName,
        ResultSnapshotState: snapshot.State,
        ActualColumns: snapshot.Columns,
        ActualRows: snapshot.Rows,
        ReturnedRowCount: snapshot.ReturnedRowCount,
        IsResultTruncated: snapshot.IsTruncated,
        ResultRowLimit: snapshot.RowLimit,
        ResultSnapshotCreatedAt: snapshot.CreatedAt,
        ResultSnapshotExpiresAt: snapshot.ExpiresAt,
        Scoring: scoring);

    internal static string? ToPublicError(Attempt attempt) => attempt.Status switch
    {
        ExecutionStatus.TimedOut => "Превышено допустимое время выполнения запроса.",
        ExecutionStatus.Error => "SQL-запрос не удалось выполнить. Проверьте синтаксис и повторите попытку.",
        _ => null
    };
}
