using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Application.Persistence;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.Training.Topics;

public record DeleteTopicCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteTopicHandler(IApplicationDbContext db)
    : IRequestHandler<DeleteTopicCommand, Result>
{
    public async Task<Result> Handle(DeleteTopicCommand command, CancellationToken ct)
    {
        var entity = await db.Topics.FirstOrDefaultAsync(t => t.Id == command.Id, ct);
        if (entity is null)
        {
            return Result.Fail(TopicErrors.NotFound(command.Id));
        }

        if (await db.Topics.AnyAsync(t => t.ParentTopicId == command.Id, ct))
        {
            return Result.Fail(TopicErrors.HasSubtopics);
        }

        if (await db.SqlTasks.AnyAsync(t => t.TopicId == command.Id, ct))
        {
            return Result.Fail(TopicErrors.HasTasks);
        }

        db.Topics.Remove(entity);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
