using Contracts;
using Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using TemplateApi.Data.Core;
using TemplateApi.Host.Common;

namespace TemplateApi.Host.Features.Templates.GetTemplateById;

public sealed class GetTemplateByIdEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Template.ByIdRoute, Handle)
            .WithName("GetTemplateById")
            .Produces<Response>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    public record Response(Guid Id, string? TemplateName);

    private static async Task<Ok<Response>> Handle(
        Guid id,
        TemplateDbContext db,
        CancellationToken ct)
    {
        var entity = await db.TemplateObjects
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException(nameof(TemplateObject), id);

        return TypedResults.Ok(new Response(entity.Id, entity.TemplateName));
    }
}
