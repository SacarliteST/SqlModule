using Contracts;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TemplateApi.Data.Core;
using TemplateApi.Host.Common;

namespace TemplateApi.Host.Features.Templates.GetAllTemplates;

public sealed class GetAllTemplatesEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Template.CollectionRoute, Handle)
            .WithName("GetAllTemplates")
            .Produces<PageResponse<TemplateItem>>()
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<Request>>();
    }

    public record Request([FromQuery] int Offset = 0, [FromQuery] int Limit = 20);

    public record TemplateItem(Guid Id, string? TemplateName);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Limit).GreaterThan(0).LessThanOrEqualTo(100);
            RuleFor(x => x.Offset).GreaterThanOrEqualTo(0);
        }
    }

    private static async Task<Ok<PageResponse<TemplateItem>>> Handle(
        [AsParameters] Request request,
        TemplateDbContext db,
        CancellationToken ct)
    {
        var total = await db.TemplateObjects.CountAsync(ct);

        var items = await db.TemplateObjects
            .AsNoTracking()
            .OrderBy(t => t.Id)
            .Skip(request.Offset)
            .Take(request.Limit)
            .ToListAsync(ct);

        return TypedResults.Ok(new PageResponse<TemplateItem>
        {
            Items = items.Select(t => new TemplateItem(t.Id, t.TemplateName)).ToList(),
            Count = total
        });
    }
}
