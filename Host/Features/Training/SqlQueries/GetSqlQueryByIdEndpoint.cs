using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Domain;
using SQLModule.Domain.Exceptions;
using SQLModule.Domain.Training;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.Training.SqlQueries;

public sealed class GetSqlQueryByIdEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Training.SqlQueries.ById, Handle)
            .WithName("GetSqlQueryById")
            .WithTags("Training")
            .Produces<CreateSqlQueryEndpoint.Response>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<Ok<CreateSqlQueryEndpoint.Response>> Handle(
        Guid id, AppDbContext db, CancellationToken ct)
    {
        var entity = await db.SqlQueries
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(SqlQuery), id);

        return TypedResults.Ok(CreateSqlQueryEndpoint.ToResponse(entity));
    }
}
