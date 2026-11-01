using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Persistence;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Attempt;

namespace SQLModule.Application.Training.Attempts;

public record GetAttemptByIdQuery(Guid Id) : IRequest<Result<AttemptResponse>>;

public sealed class GetAttemptByIdHandler(
    IApplicationDbContext db,
    IAttemptResultSnapshotService snapshotService,
    IAttemptScoringReadService scoringReadService,
    TimeProvider timeProvider)
    : IRequestHandler<GetAttemptByIdQuery, Result<AttemptResponse>>
{
    public async Task<Result<AttemptResponse>> Handle(GetAttemptByIdQuery query, CancellationToken ct)
    {
        var entity = await db.Attempts.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.Id, ct);

        if (entity is null)
        {
            return Result<AttemptResponse>.Fail(AttemptErrors.NotFound(query.Id));
        }

        var snapshot = snapshotService.Read(entity, timeProvider.GetUtcNow());
        var scoring = await scoringReadService.ReadAsync(entity, studentSafe: false, ct);
        return AttemptMappings.ToResponse(entity, snapshot, scoring);
    }
}
