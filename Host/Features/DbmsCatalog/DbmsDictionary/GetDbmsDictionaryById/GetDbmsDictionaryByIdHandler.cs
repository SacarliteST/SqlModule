using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts.DbmsCatalog.DbmsDictionary;
using SQLModule.Data.Core;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.DbmsCatalog.DbmsDictionary.GetDbmsDictionaryById;

internal record GetDbmsDictionaryByIdQuery(Guid Id) : IRequest<Result<DbmsDictionaryResponse>>;

internal sealed class GetDbmsDictionaryByIdHandler(AppDbContext db)
    : IRequestHandler<GetDbmsDictionaryByIdQuery, Result<DbmsDictionaryResponse>>
{
    public async Task<Result<DbmsDictionaryResponse>> Handle(
        GetDbmsDictionaryByIdQuery query, CancellationToken ct)
    {
        var entity = await db.DbmsDictionaries
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.Id, ct);

        if (entity is null)
        {
            return Result<DbmsDictionaryResponse>.Fail(DbmsDictionaryErrors.NotFound(query.Id));
        }

        return DbmsDictionaryMappings.ToResponse(entity);
    }
}
