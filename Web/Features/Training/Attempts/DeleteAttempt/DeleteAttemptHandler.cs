using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Data.Core;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.Attempts;

internal record DeleteAttemptCommand(Guid Id) : IRequest<Result>;

internal sealed class DeleteAttemptHandler(AppDbContext db)
    : IRequestHandler<DeleteAttemptCommand, Result>
{
    public async Task<Result> Handle(DeleteAttemptCommand command, CancellationToken ct)
    {
        var entity = await db.Attempts.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (entity is null)
        {
            return Result.Fail(AttemptErrors.NotFound(command.Id));
        }

        db.Attempts.Remove(entity);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
