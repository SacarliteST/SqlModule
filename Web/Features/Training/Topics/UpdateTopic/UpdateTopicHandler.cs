using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Data.Core;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.Topics;

internal record UpdateTopicCommand(Guid Id, string TopicName) : IRequest<Result>;

internal sealed class UpdateTopicHandler(AppDbContext db)
    : IRequestHandler<UpdateTopicCommand, Result>
{
    public async Task<Result> Handle(UpdateTopicCommand command, CancellationToken ct)
    {
        var entity = await db.Topics.FirstOrDefaultAsync(t => t.Id == command.Id, ct);
        if (entity is null)
        {
            return Result.Fail(TopicErrors.NotFound(command.Id));
        }

        entity.Update(command.TopicName);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
