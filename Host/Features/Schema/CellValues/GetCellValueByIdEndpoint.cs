using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Domain;
using SQLModule.Domain.Exceptions;
using SQLModule.Domain.Schema;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.Schema.CellValues;

public sealed class GetCellValueByIdEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Schema.CellValues.ById, Handle)
            .WithName("GetCellValueById")
            .WithTags("Schema")
            .Produces<CreateCellValueEndpoint.Response>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<Ok<CreateCellValueEndpoint.Response>> Handle(
        Guid id, AppDbContext db, CancellationToken ct)
    {
        var entity = await db.CellValues
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(CellValue), id);

        return TypedResults.Ok(CreateCellValueEndpoint.ToResponse(entity));
    }
}
