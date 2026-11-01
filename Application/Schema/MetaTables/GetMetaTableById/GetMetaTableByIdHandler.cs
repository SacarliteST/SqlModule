using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Persistence;
using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.MetaTable;

namespace SQLModule.Application.Schema.MetaTables;

public record GetMetaTableByIdQuery(Guid Id) : IRequest<Result<MetaTableResponse>>;

public sealed class GetMetaTableByIdHandler(IApplicationDbContext db)
    : IRequestHandler<GetMetaTableByIdQuery, Result<MetaTableResponse>>
{
    public async Task<Result<MetaTableResponse>> Handle(GetMetaTableByIdQuery query, CancellationToken ct)
    {
        var entity = await db.MetaTables.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.Id, ct);

        if (entity is null)
        {
            return Result<MetaTableResponse>.Fail(MetaTableErrors.NotFound(query.Id));
        }

        return MetaTableMappings.ToResponse(entity);
    }
}
