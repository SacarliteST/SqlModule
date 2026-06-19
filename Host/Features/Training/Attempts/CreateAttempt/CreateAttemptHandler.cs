using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts.Training.Attempt;
using SQLModule.Data.Core;
using SQLModule.Domain.Training;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Training.Attempts;

internal record CreateAttemptCommand(
    Guid UserId, bool IsSuccess,
    DateTimeOffset StartAttempt, DateTimeOffset EndAttempt,
    Guid TaskId, Guid QueryId)
    : IRequest<Result<AttemptResponse>>;

internal sealed class CreateAttemptHandler(AppDbContext db)
    : IRequestHandler<CreateAttemptCommand, Result<AttemptResponse>>
{
    public async Task<Result<AttemptResponse>> Handle(CreateAttemptCommand command, CancellationToken ct)
    {
        if (!await db.SqlTasks.AnyAsync(t => t.Id == command.TaskId, ct))
        {
            return Result<AttemptResponse>.Fail(AttemptErrors.TaskNotFound(command.TaskId));
        }

        if (!await db.SqlQueries.AnyAsync(q => q.Id == command.QueryId, ct))
        {
            return Result<AttemptResponse>.Fail(AttemptErrors.QueryNotFound(command.QueryId));
        }

        var entity = Attempt.Create(
            command.UserId, command.IsSuccess,
            command.StartAttempt, command.EndAttempt,
            command.TaskId, command.QueryId);

        db.Attempts.Add(entity);
        await db.SaveChangesAsync(ct);
        return AttemptMappings.ToResponse(entity);
    }
}
