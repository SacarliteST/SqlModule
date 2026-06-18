using SQLModule.Domain.Schema;
using SQLModule.Domain.Exceptions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Domain;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.Schema.MetaRelationships;

public sealed class GetMetaRelationshipByIdEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Schema.MetaRelationships.ById, Handle)
            .WithName("GetMetaRelationshipById")
            .WithTags("Schema")
            .Produces<CreateMetaRelationshipEndpoint.Response>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<Ok<CreateMetaRelationshipEndpoint.Response>> Handle(
        Guid id, TemplateDbContext db, CancellationToken ct)
    {
        var entity = await db.MetaRelationships
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(MetaRelationship), id);

        return TypedResults.Ok(CreateMetaRelationshipEndpoint.ToResponse(entity));
    }
}
