using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.TargetDb;
using SQLModule.Application.Persistence;
using SQLModule.Domain.Schema;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.Schema.TargetDbs;

public record GetTargetDbByIdQuery(Guid Id) : IRequest<Result<TargetDbResponse>>;

public sealed class GetTargetDbByIdHandler(IApplicationDbContext db)
    : IRequestHandler<GetTargetDbByIdQuery, Result<TargetDbResponse>>
{
    public async Task<Result<TargetDbResponse>> Handle(GetTargetDbByIdQuery query, CancellationToken ct)
    {
        var entity = await db.TargetDbs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.Id, ct);

        if (entity is null)
        {
            return Result<TargetDbResponse>.Fail(TargetDbErrors.NotFound(query.Id));
        }

        return TargetDbMappings.ToResponse(entity);
    }
}
