using Contracts;
using Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using TemplateApi.Data.Core;
using TemplateApi.Host.Common;

namespace TemplateApi.Host.Features.Templates.DeleteTemplate;

public sealed class DeleteTemplateEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapDelete(ApiRoutes.Template.ByIdRoute, Handle)
            .WithName("DeleteTemplate")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<NoContent> Handle(
        Guid id,
        TemplateDbContext db,
        ILogger<DeleteTemplateEndpoint> logger,
        CancellationToken ct)
    {
        var entity = await db.TemplateObjects
            .FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException(nameof(TemplateObject), id);

        db.TemplateObjects.Remove(entity);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Шаблонный объект с Id: {Id} удалён из БД", entity.Id);

        return TypedResults.NoContent();
    }
}
