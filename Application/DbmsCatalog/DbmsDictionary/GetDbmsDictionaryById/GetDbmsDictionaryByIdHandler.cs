using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.DbmsCatalog.DbmsDictionary;
using SQLModule.Application.Persistence;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.DbmsCatalog.DbmsDictionary.GetDbmsDictionaryById;

public record GetDbmsDictionaryByIdQuery(Guid Id) : IRequest<Result<DbmsDictionaryResponse>>;

public sealed class GetDbmsDictionaryByIdHandler(IApplicationDbContext db)
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
