using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.DbmsCatalog.PhysicalType;
using SQLModule.Application.Persistence;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.DbmsCatalog.PhysicalTypes;

public record GetPhysicalTypeByIdQuery(Guid Id) : IRequest<Result<PhysicalTypeResponse>>;

public sealed class GetPhysicalTypeByIdHandler(IApplicationDbContext db)
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
