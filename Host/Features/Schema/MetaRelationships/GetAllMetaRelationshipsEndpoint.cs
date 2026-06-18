using SQLModule.Domain.Schema;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.Schema.MetaRelationships;

public sealed class GetAllMetaRelationshipsEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Schema.MetaRelationships.Collection, Handle)
            .WithName("GetAllMetaRelationships")
            .WithTags("Schema")
            .Produces<PageResponse<CreateMetaRelationshipEndpoint.Response>>()
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<Request>>();
    }

    public record Request([FromQuery] int Offset = 0, [FromQuery] int Limit = 20);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Limit).GreaterThan(0).LessThanOrEqualTo(100);
            RuleFor(x => x.Offset).GreaterThanOrEqualTo(0);
        }
    }

    private static async Task<Ok<PageResponse<CreateMetaRelationshipEndpoint.Response>>> Handle(
        [AsParameters] Request request, TemplateDbContext db, CancellationToken ct)
    {
        var total = await db.MetaRelationships.CountAsync(ct);

        var entities = await db.MetaRelationships
            .AsNoTracking()
            .OrderBy(x => x.RelationshipName)
            .Skip(request.Offset)
            .Take(request.Limit)
            .ToListAsync(ct);

        var items = entities.Select(CreateMetaRelationshipEndpoint.ToResponse).ToList();

        return TypedResults.Ok(new PageResponse<CreateMetaRelationshipEndpoint.Response> { Items = items, Count = total });
    }
}
