using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.DbmsCatalog.PhysicalType;
using SQLModule.Data.Core;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.DbmsCatalog.PhysicalTypes;

internal record GetPhysicalTypeByIdQuery(Guid Id) : IRequest<Result<PhysicalTypeResponse>>;

internal sealed class GetPhysicalTypeByIdHandler(AppDbContext db)
    : IRequestHandler<GetPhysicalTypeByIdQuery, Result<PhysicalTypeResponse>>
{
    public async Task<Result<PhysicalTypeResponse>> Handle(
        GetPhysicalTypeByIdQuery query, CancellationToken ct)
    {
        var entity = await db.PhysicalTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.Id, ct);

        if (entity is null)
        {
            return Result<PhysicalTypeResponse>.Fail(PhysicalTypeErrors.NotFound(query.Id));
        }

        return PhysicalTypeMappings.ToResponse(entity);
    }
}
