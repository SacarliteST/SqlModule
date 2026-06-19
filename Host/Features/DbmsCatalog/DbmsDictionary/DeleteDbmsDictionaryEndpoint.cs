using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Domain;
using SQLModule.Domain.DbmsCatalog;
using SQLModule.Domain.Exceptions;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.DbmsCatalog.DbmsDictionary;

public sealed class DeleteDbmsDictionaryEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapDelete(ApiRoutes.DbmsCatalog.DbmsDictionaries.ById, Handle)
            .WithName("DeleteDbmsDictionary")
            .WithTags("DbmsCatalog")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<NoContent> Handle(
        Guid id, AppDbContext db, CancellationToken ct)
    {
        var entity = await db.DbmsDictionaries
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("DbmsDictionary", id);

        db.DbmsDictionaries.Remove(entity);
        await db.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}
