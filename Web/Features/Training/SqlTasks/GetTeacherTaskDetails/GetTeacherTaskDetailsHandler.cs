using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.SqlTask;
using SQLModule.Data.Core;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.SqlTasks.GetTeacherTaskDetails;

internal sealed record GetTeacherTaskDetailsQuery(Guid TaskId)
    : IRequest<Result<TeacherTaskDetailsResponse>>;

internal sealed class GetTeacherTaskDetailsHandler(AppDbContext db)
    : IRequestHandler<GetTeacherTaskDetailsQuery, Result<TeacherTaskDetailsResponse>>
{
    private const int LastAttemptsLimit = 5;

    public async Task<Result<TeacherTaskDetailsResponse>> Handle(
        GetTeacherTaskDetailsQuery query,
        CancellationToken ct)
    {
        var task = await (
            from sqlTask in db.SqlTasks.AsNoTracking()
            join topic in db.Topics.AsNoTracking() on sqlTask.TopicId equals topic.Id
            join sqlQuery in db.SqlQueries.AsNoTracking() on sqlTask.SqlQueryId equals sqlQuery.Id
            join targetDb in db.TargetDbs.AsNoTracking() on sqlQuery.TargetDbId equals targetDb.Id
            join dbms in db.DbmsDictionaries.AsNoTracking() on targetDb.DbmsId equals dbms.Id
            where sqlTask.Id == query.TaskId
            select new
            {
                SqlTask = sqlTask,
                TopicName = topic.TopicName,
                SqlQuery = sqlQuery,
                TargetDb = targetDb,
                DbmsName = dbms.DbmsName
            }).FirstOrDefaultAsync(ct);

        if (task is null)
        {
            return Result<TeacherTaskDetailsResponse>.Fail(SqlTaskErrors.NotFound(query.TaskId));
        }

        var tables = await db.MetaTables.AsNoTracking()
            .Where(table => table.TargetDbId == task.TargetDb.Id)
            .OrderBy(table => table.TableName)
            .Select(table => new TeacherTaskTableResponse(
                table.TableName,
                db.MetaAttributes.Count(attribute => attribute.MetaTableId == table.Id)))
            .ToListAsync(ct);

        var attemptsQuery = db.Attempts.AsNoTracking()
            .Where(attempt => attempt.TaskId == query.TaskId);

        var attemptsCount = await attemptsQuery.CountAsync(ct);
        var lastAttempts = await attemptsQuery
            .OrderByDescending(attempt => attempt.FinishedAt)
            .Take(LastAttemptsLimit)
            .Select(attempt => new TeacherTaskAttemptResponse(
                attempt.Id,
                attempt.UserId,
                attempt.StudentName,
                attempt.IsCorrect,
                attempt.Status,
                attempt.DurationMs,
                attempt.FinishedAt))
            .ToListAsync(ct);

        return new TeacherTaskDetailsResponse(
            task.SqlTask.Id,
            task.SqlTask.TopicId,
            task.TopicName,
            task.SqlTask.TaskName,
            task.SqlTask.TaskText,
            task.SqlTask.DifficultyLevel,
            task.SqlTask.PublicationStatus,
            task.SqlTask.CreatedAt,
            task.SqlTask.UpdatedAt,
            task.SqlTask.CreatedById,
            new TeacherTaskSqlQueryResponse(
                task.SqlQuery.Id,
                task.SqlQuery.QueryText,
                task.SqlQuery.StrictColumnOrder,
                task.SqlQuery.StrictRowOrder),
            new TeacherTaskTargetDbResponse(
                task.TargetDb.Id,
                task.TargetDb.DbName,
                task.DbmsName,
                tables),
            attemptsCount,
            lastAttempts);
    }
}
