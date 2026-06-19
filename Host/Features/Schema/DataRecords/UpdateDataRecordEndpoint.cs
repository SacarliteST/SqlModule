using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Domain;
using SQLModule.Domain.Exceptions;
using SQLModule.Domain.Schema;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.Schema.DataRecords;

public sealed class UpdateDataRecordEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiRoutes.Schema.DataRecords.ById, Handle)
            .WithName("UpdateDataRecord")
            .WithTags("Schema")
            .Produces<CreateDataRecordEndpoint.Response>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    public record Request(int? SortOrder);

    private static async Task<Ok<CreateDataRecordEndpoint.Response>> Handle(
        Guid id, Request request, AppDbContext db, CancellationToken ct)
    {
        var entity = await db.DataRecords
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(DataRecord), id);

        entity.Update(request.SortOrder);
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(CreateDataRecordEndpoint.ToResponse(entity));
    }
}
