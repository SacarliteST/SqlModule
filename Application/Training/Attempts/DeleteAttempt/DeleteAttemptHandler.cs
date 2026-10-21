using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Application.Persistence;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.Training.Attempts;

public record DeleteAttemptCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteAttemptHandler(IApplicationDbContext db)
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
