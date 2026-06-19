using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Domain;
using SQLModule.Domain.Exceptions;
using SQLModule.Domain.Schema;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.Schema.MetaTables;

public sealed class GetMetaTableByIdEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Schema.MetaTables.ById, Handle)
            .WithName("GetMetaTableById")
            .WithTags("Schema")
            .Produces<CreateMetaTableEndpoint.Response>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<Ok<CreateMetaTableEndpoint.Response>> Handle(
        Guid id, AppDbContext db, CancellationToken ct)
    {
        var entity = await db.MetaTables
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(MetaTable), id);

        return TypedResults.Ok(CreateMetaTableEndpoint.ToResponse(entity));
    }
}
