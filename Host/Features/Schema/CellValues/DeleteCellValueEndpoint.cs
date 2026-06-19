using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Domain;
using SQLModule.Domain.Exceptions;
using SQLModule.Domain.Schema;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.Schema.CellValues;

public sealed class DeleteCellValueEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapDelete(ApiRoutes.Schema.CellValues.ById, Handle)
            .WithName("DeleteCellValue")
            .WithTags("Schema")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<NoContent> Handle(
        Guid id, AppDbContext db, CancellationToken ct)
    {
        var entity = await db.CellValues
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(CellValue), id);

        db.CellValues.Remove(entity);
        await db.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}
