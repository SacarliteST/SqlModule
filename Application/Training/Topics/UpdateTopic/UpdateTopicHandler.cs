using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Application.Persistence;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.Training.Topics;

public record UpdateTopicCommand(Guid Id, string TopicName, string? Description) : IRequest<Result>;

public sealed class UpdateTopicHandler(IApplicationDbContext db)
    : IRequestHandler<UpdateTopicCommand, Result>
{
    public async Task<Result> Handle(UpdateTopicCommand command, CancellationToken ct)
    {
        var entity = await db.Topics.FirstOrDefaultAsync(t => t.Id == command.Id, ct);
        if (entity is null)
        {
            return Result.Fail(TopicErrors.NotFound(command.Id));
        }

        entity.Update(command.TopicName, command.Description);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
