using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Domain;
using SQLModule.Domain.Exceptions;
using SQLModule.Domain.Schema;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.Schema.AttributeParameterValues;

public sealed class DeleteAttributeParameterValueEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapDelete(ApiRoutes.Schema.AttributeParameterValues.ById, Handle)
            .WithName("DeleteAttributeParameterValue")
            .WithTags("Schema")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<NoContent> Handle(
        Guid id, AppDbContext db, CancellationToken ct)
    {
        var entity = await db.AttributeParameterValues
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(AttributeParameterValue), id);

        db.AttributeParameterValues.Remove(entity);
        await db.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}
