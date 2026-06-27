using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.TargetDb;
using SQLModule.Data.Core;
using SQLModule.Domain.Schema;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.TargetDbs;

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
            return Result<TargetDbResponse>.Fail(TargetDbErrors.NotFound(query.Id));
        }

        return TargetDbMappings.ToResponse(entity);
    }
}
