using SQLModule.Domain.Training;
using SQLModule.Domain.Exceptions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Domain;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.Training.Attempts;

public sealed class GetAttemptByIdEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Training.Attempts.ById, Handle)
            .WithName("GetAttemptById")
            .WithTags("Training")
            .Produces<CreateAttemptEndpoint.Response>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<Ok<CreateAttemptEndpoint.Response>> Handle(
        Guid id, TemplateDbContext db, CancellationToken ct)
    {
        var entity = await db.Attempts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(Attempt), id);

        return TypedResults.Ok(CreateAttemptEndpoint.ToResponse(entity));
    }
}
