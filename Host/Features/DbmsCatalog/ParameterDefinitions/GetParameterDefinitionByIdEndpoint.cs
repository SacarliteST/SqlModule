using SQLModule.Domain.DbmsCatalog;
using SQLModule.Domain.Exceptions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Domain;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.DbmsCatalog.ParameterDefinitions;

public sealed class GetParameterDefinitionByIdEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.DbmsCatalog.ParameterDefinitions.ById, Handle)
            .WithName("GetParameterDefinitionById")
            .WithTags("DbmsCatalog")
            .Produces<CreateParameterDefinitionEndpoint.Response>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<Ok<CreateParameterDefinitionEndpoint.Response>> Handle(
        Guid id, AppDbContext db, CancellationToken ct)
    {
        var entity = await db.ParameterDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(ParameterDefinition), id);

        return TypedResults.Ok(CreateParameterDefinitionEndpoint.ToResponse(entity));
    }
}
