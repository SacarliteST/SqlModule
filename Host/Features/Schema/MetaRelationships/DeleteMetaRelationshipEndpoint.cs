using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Domain;
using SQLModule.Domain.Exceptions;
using SQLModule.Domain.Schema;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.Schema.MetaRelationships;

public sealed class DeleteMetaRelationshipEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapDelete(ApiRoutes.Schema.MetaRelationships.ById, Handle)
            .WithName("DeleteMetaRelationship")
            .WithTags("Schema")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<NoContent> Handle(
        Guid id, AppDbContext db, CancellationToken ct)
    {
        var entity = await db.MetaRelationships
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(MetaRelationship), id);

        db.MetaRelationships.Remove(entity);
        await db.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}
