using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.MetaRelationship;
using SQLModule.Application.Persistence;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.Schema.MetaRelationships;

public record GetMetaRelationshipByIdQuery(Guid Id) : IRequest<Result<MetaRelationshipResponse>>;

public sealed class GetMetaRelationshipByIdHandler(IApplicationDbContext db)
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
