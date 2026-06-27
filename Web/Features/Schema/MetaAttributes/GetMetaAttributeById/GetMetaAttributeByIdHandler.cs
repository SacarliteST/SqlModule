using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.MetaAttribute;
using SQLModule.Data.Core;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.MetaAttributes;

internal record GetMetaAttributeByIdQuery(Guid Id) : IRequest<Result<MetaAttributeResponse>>;

internal sealed class GetMetaAttributeByIdHandler(AppDbContext db)
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
