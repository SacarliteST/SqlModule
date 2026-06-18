using SQLModule.Domain.Schema;
using SQLModule.Domain.Exceptions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Domain;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.Schema.MetaAttributes;

public sealed class GetMetaAttributeByIdEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Schema.MetaAttributes.ById, Handle)
            .WithName("GetMetaAttributeById")
            .WithTags("Schema")
            .Produces<CreateMetaAttributeEndpoint.Response>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<Ok<CreateMetaAttributeEndpoint.Response>> Handle(
        Guid id, TemplateDbContext db, CancellationToken ct)
    {
        var entity = await db.MetaAttributes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(MetaAttribute), id);

        return TypedResults.Ok(CreateMetaAttributeEndpoint.ToResponse(entity));
    }
}
