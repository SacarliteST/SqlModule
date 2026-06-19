using Microsoft.EntityFrameworkCore;
using SQLModule.Data.Core;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Training.Attempts;

internal record DeleteAttemptCommand(Guid Id) : IRequest<Result>;

internal sealed class DeleteAttemptHandler(AppDbContext db)
    : IRequestHandler<DeleteAttemptCommand, Result>
{
    public async Task<Result> Handle(DeleteAttemptCommand command, CancellationToken ct)
    {
        var entity = await db.Attempts.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (entity is null)
        {
            return Result.Fail(Error.NotFound("Attempt", command.Id));
        }

        db.Attempts.Remove(entity);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
