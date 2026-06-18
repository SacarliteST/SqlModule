using SQLModule.Domain.Training;
using SQLModule.Domain.Exceptions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Domain;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.Training.Topics;

public sealed class MoveTopicEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiRoutes.Training.Topics.Parent, Handle)
            .WithName("MoveTopic")
            .WithTags("Training")
            .Produces<CreateTopicEndpoint.Response>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    public record Request(Guid? ParentTopicId);

    private static async Task<Ok<CreateTopicEndpoint.Response>> Handle(
        Guid id, Request request, TemplateDbContext db, CancellationToken ct)
    {
        var entity = await db.Topics
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(Topic), id);

        if (request.ParentTopicId.HasValue &&
            !await db.Topics.AnyAsync(x => x.Id == request.ParentTopicId.Value, ct))
        {
            throw new NotFoundException(nameof(Topic), request.ParentTopicId.Value);
        }

        entity.Move(request.ParentTopicId);
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(CreateTopicEndpoint.ToResponse(entity));
    }
}
