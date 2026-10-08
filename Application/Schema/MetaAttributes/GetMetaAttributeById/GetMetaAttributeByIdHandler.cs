using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.MetaAttribute;
using SQLModule.Application.Persistence;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.Schema.MetaAttributes;

public record GetMetaAttributeByIdQuery(Guid Id) : IRequest<Result<MetaAttributeResponse>>;

public sealed class GetMetaAttributeByIdHandler(IApplicationDbContext db)
    : IRequestHandler<GetMetaAttributeByIdQuery, Result<MetaAttributeResponse>>
{
    public async Task<Result<MetaAttributeResponse>> Handle(
        GetMetaAttributeByIdQuery query, CancellationToken ct)
    {
        var entity = await db.MetaAttributes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.Id, ct);

        if (entity is null)
        {
            return Result<MetaAttributeResponse>.Fail(MetaAttributeErrors.NotFound(query.Id));
        }

        return MetaAttributeMappings.ToResponse(entity);
    }
}
