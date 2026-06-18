using SQLModule.Domain.Schema;
using SQLModule.Domain.Exceptions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Domain;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.Schema.CellValues;

public sealed class UpdateCellValueEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiRoutes.Schema.CellValues.ById, Handle)
            .WithName("UpdateCellValue")
            .WithTags("Schema")
            .Produces<CreateCellValueEndpoint.Response>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    public record Request(string? TextValue);

    private static async Task<Ok<CreateCellValueEndpoint.Response>> Handle(
        Guid id, Request request, AppDbContext db, CancellationToken ct)
    {
        var entity = await db.CellValues
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(CellValue), id);

        entity.Update(request.TextValue);
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(CreateCellValueEndpoint.ToResponse(entity));
    }
}
