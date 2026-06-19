using Microsoft.EntityFrameworkCore;
using SQLModule.Data.Core;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

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
            return Result.Fail(Error.NotFound("Topic", command.Id));
        }

        if (await db.Topics.AnyAsync(t => t.ParentTopicId == command.Id, ct))
        {
            return Result.Fail(Error.Conflict(
                "Topic.HasChildren",
                $"Тема '{command.Id}' содержит подтемы и не может быть удалена."));
        }

        if (await db.SqlTasks.AnyAsync(t => t.TopicId == command.Id, ct))
        {
            return Result.Fail(Error.Conflict(
                "Topic.HasChildren",
                $"Тема '{command.Id}' содержит задания и не может быть удалена."));
        }

        db.Topics.Remove(entity);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
