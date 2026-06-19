using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts.Training.Attempt;
using SQLModule.Data.Core;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Training.Attempts;

internal record GetAttemptByIdQuery(Guid Id) : IRequest<Result<AttemptResponse>>;

internal sealed class GetAttemptByIdHandler(AppDbContext db)
    : IRequestHandler<GetAttemptByIdQuery, Result<AttemptResponse>>
{
    public async Task<Result<AttemptResponse>> Handle(GetAttemptByIdQuery query, CancellationToken ct)
    {
        var entity = await db.Attempts.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.Id, ct);

        if (entity is null)
        {
            return Result<AttemptResponse>.Fail(Error.NotFound("Attempt", query.Id));
        }

        return AttemptMappings.ToResponse(entity);
    }
}
