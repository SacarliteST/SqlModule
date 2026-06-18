using SQLModule.Domain.Schema;
using SQLModule.Domain.Exceptions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Domain;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.Schema.TargetDbs;

public sealed class GetTargetDbByIdEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Schema.TargetDbs.ById, Handle)
            .WithName("GetTargetDbById")
            .WithTags("Schema")
            .Produces<CreateTargetDbEndpoint.Response>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<Ok<CreateTargetDbEndpoint.Response>> Handle(
        Guid id, TemplateDbContext db, CancellationToken ct)
    {
        var entity = await db.TargetDbs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(TargetDb), id);

        return TypedResults.Ok(CreateTargetDbEndpoint.ToResponse(entity));
    }
}
