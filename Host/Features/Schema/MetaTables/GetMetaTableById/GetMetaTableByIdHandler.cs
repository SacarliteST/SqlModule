using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.MetaTable;
using SQLModule.Data.Core;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.Schema.MetaTables;

internal record GetMetaTableByIdQuery(Guid Id) : IRequest<Result<MetaTableResponse>>;

internal sealed class GetMetaTableByIdHandler(AppDbContext db)
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
