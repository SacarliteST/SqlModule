using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts.Schema.TargetDb;
using SQLModule.Data.Core;
using SQLModule.Domain.Schema;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Schema.TargetDbs;

internal record GetTargetDbByIdQuery(Guid Id) : IRequest<Result<TargetDbResponse>>;

internal sealed class GetTargetDbByIdHandler(AppDbContext db)
    : IRequestHandler<GetTargetDbByIdQuery, Result<TargetDbResponse>>
{
    public async Task<Result<TargetDbResponse>> Handle(GetTargetDbByIdQuery query, CancellationToken ct)
    {
        var entity = await db.TargetDbs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.Id, ct);

        if (entity is null)
        {
            return Result<TargetDbResponse>.Fail(Error.NotFound(nameof(TargetDb), query.Id));
        }

        return TargetDbMappings.ToResponse(entity);
    }
}
