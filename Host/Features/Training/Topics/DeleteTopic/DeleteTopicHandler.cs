using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Data.Core;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.Training.Topics;

internal record DeleteTopicCommand(Guid Id) : IRequest<Result>;

internal sealed class DeleteTopicHandler(AppDbContext db)
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
