using Microsoft.EntityFrameworkCore;
using SQLModule.Data.Core;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Training.Attempts;

internal record UpdateAttemptCommand(Guid Id, bool IsSuccess, DateTimeOffset EndAttempt)
    : IRequest<Result>;

internal sealed class UpdateAttemptHandler(AppDbContext db)
    : IRequestHandler<UpdateAttemptCommand, Result>
{
    public async Task<Result> Handle(UpdateAttemptCommand command, CancellationToken ct)
    {
        var entity = await db.Attempts.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (entity is null)
        {
            return Result.Fail(AttemptErrors.NotFound(command.Id));
        }

        if (command.EndAttempt < entity.StartAttempt)
        {
            return Result.Fail(AttemptErrors.InvalidTimeRange);
        }

        entity.Update(command.IsSuccess, command.EndAttempt);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
