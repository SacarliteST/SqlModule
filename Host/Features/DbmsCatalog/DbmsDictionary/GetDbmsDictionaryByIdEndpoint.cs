using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Domain;
using SQLModule.Domain.DbmsCatalog;
using SQLModule.Domain.Exceptions;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.DbmsCatalog.DbmsDictionary;

public sealed class GetDbmsDictionaryByIdEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.DbmsCatalog.DbmsDictionaries.ById, Handle)
            .WithName("GetDbmsDictionaryById")
            .WithTags("DbmsCatalog")
            .Produces<CreateDbmsDictionaryEndpoint.Response>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<Ok<CreateDbmsDictionaryEndpoint.Response>> Handle(
        Guid id, AppDbContext db, CancellationToken ct)
    {
        var entity = await db.DbmsDictionaries
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("DbmsDictionary", id);

        return TypedResults.Ok(CreateDbmsDictionaryEndpoint.ToResponse(entity));
    }
}
