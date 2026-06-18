using SQLModule.Domain.DbmsCatalog;
using SQLModule.Domain.Exceptions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Domain;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.DbmsCatalog.PhysicalTypes;

public sealed class GetPhysicalTypeByIdEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.DbmsCatalog.PhysicalTypes.ById, Handle)
            .WithName("GetPhysicalTypeById")
            .WithTags("DbmsCatalog")
            .Produces<CreatePhysicalTypeEndpoint.Response>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<Ok<CreatePhysicalTypeEndpoint.Response>> Handle(
        Guid id, AppDbContext db, CancellationToken ct)
    {
        var entity = await db.PhysicalTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(PhysicalType), id);

        return TypedResults.Ok(CreatePhysicalTypeEndpoint.ToResponse(entity));
    }
}
