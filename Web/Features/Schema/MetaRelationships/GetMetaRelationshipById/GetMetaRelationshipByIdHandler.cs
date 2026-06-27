using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.MetaRelationship;
using SQLModule.Data.Core;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.MetaRelationships;

internal record GetMetaRelationshipByIdQuery(Guid Id) : IRequest<Result<MetaRelationshipResponse>>;

internal sealed class GetMetaRelationshipByIdHandler(AppDbContext db)
    : IRequestHandler<GetMetaRelationshipByIdQuery, Result<MetaRelationshipResponse>>
{
    public async Task<Result<MetaRelationshipResponse>> Handle(
        GetMetaRelationshipByIdQuery query, CancellationToken ct)
    {
        var entity = await db.MetaRelationships
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.Id, ct);

        if (entity is null)
        {
            return Result<MetaRelationshipResponse>.Fail(MetaRelationshipErrors.NotFound(query.Id));
        }

        return MetaRelationshipMappings.ToResponse(entity);
    }
}
