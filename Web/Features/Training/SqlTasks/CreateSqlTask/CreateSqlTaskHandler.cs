using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.SqlTask;
using SQLModule.Data.Core;
using SQLModule.Domain.Training;
using SQLModule.Sandbox;
using SQLModule.Web.Common.Cqrs;
using SQLModule.Web.Common.Sandbox;
using SQLModule.Web.Features.Training.SqlQueries;

namespace SQLModule.Web.Features.Training.SqlTasks;

internal sealed record ReferenceQueryData(
    Guid TargetDbId,
    string QueryText,
    bool StrictColumnOrder,
    bool StrictRowOrder);

internal record CreateSqlTaskCommand(
    Guid TopicId,
    string TaskName,
    string TaskText,
    short DifficultyLevel,
    ReferenceQueryData ReferenceQuery)
    : IRequest<Result<SqlTaskResponse>>;

internal sealed class CreateSqlTaskHandler(
    ISqlQueryValidationRunner validationRunner,
    AppDbContext db)
    : IRequestHandler<CreateSqlTaskCommand, Result<SqlTaskResponse>>
{
    public async Task<Result<SqlTaskResponse>> Handle(CreateSqlTaskCommand command, CancellationToken ct)
    {
        if (!await db.Topics.AnyAsync(x => x.Id == command.TopicId, ct))
        {
            return Result<SqlTaskResponse>.Fail(SqlTaskErrors.TopicNotFound(command.TopicId));
        }

        var reference = command.ReferenceQuery;
        var run = await validationRunner.ValidateAsync(reference.TargetDbId, reference.QueryText, ct);
        if (!run.IsSuccess)
        {
            return Result<SqlTaskResponse>.Fail(run.Error!);
        }

        var sqlQuery = SqlQuery.Create(
            reference.QueryText,
            reference.StrictColumnOrder,
            reference.StrictRowOrder,
            reference.TargetDbId);
        sqlQuery.SetExpectedResult(GoldenResult.Serialize(run.Value!));

        var entity = SqlTask.Create(
            command.TopicId, sqlQuery.Id,
            command.TaskName, command.TaskText, command.DifficultyLevel,
            publicationStatus: PublicationStatus.Draft);

        db.SqlQueries.Add(sqlQuery);
        db.SqlTasks.Add(entity);
        await db.SaveChangesAsync(ct);
        return SqlTaskMappings.ToResponse(entity);
    }
}
