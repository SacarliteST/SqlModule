using Microsoft.EntityFrameworkCore;
using SQLModule.Data.Core;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Training.Topics;

internal record MoveTopicCommand(Guid Id, Guid? NewParentTopicId) : IRequest<Result>;

internal sealed class MoveTopicHandler(AppDbContext db)
    : IRequestHandler<MoveTopicCommand, Result>
{
    public async Task<Result> Handle(MoveTopicCommand command, CancellationToken ct)
    {
        var entity = await db.Topics.FirstOrDefaultAsync(t => t.Id == command.Id, ct);
        if (entity is null)
        {
            return Result.Fail(Error.NotFound("Topic", command.Id));
        }

        if (command.NewParentTopicId.HasValue)
        {
            if (!await db.Topics.AnyAsync(t => t.Id == command.NewParentTopicId.Value, ct))
            {
                return Result.Fail(Error.Conflict(
                    "Topic.ParentNotFound",
                    $"Родительская тема с id '{command.NewParentTopicId}' не найдена."));
            }

            if (await WouldCreateCycleAsync(command.Id, command.NewParentTopicId.Value, ct))
            {
                return Result.Fail(Error.Conflict(
                    "Topic.CycleDetected",
                    "Перемещение создаст цикл в иерархии тем."));
            }
        }

        entity.Move(command.NewParentTopicId);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    // Поднимаемся по дереву от newParentId вверх; если встретим topicId — цикл.
    private async Task<bool> WouldCreateCycleAsync(Guid topicId, Guid newParentId, CancellationToken ct)
    {
        var currentId = (Guid?)newParentId;
        while (currentId.HasValue)
        {
            if (currentId.Value == topicId)
            {
                return true;
            }

            currentId = await db.Topics.AsNoTracking()
                .Where(t => t.Id == currentId.Value)
                .Select(t => t.ParentTopicId)
                .FirstOrDefaultAsync(ct);
        }
        return false;
    }
}
