using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Domain.Schema;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.Schema.MetaAttributes;

public sealed class GetAllMetaAttributesEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Schema.MetaAttributes.Collection, Handle)
            .WithName("GetAllMetaAttributes")
            .WithTags("Schema")
            .Produces<PageResponse<CreateMetaAttributeEndpoint.Response>>()
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

    private static async Task<Ok<PageResponse<CreateMetaAttributeEndpoint.Response>>> Handle(
        [AsParameters] Request request, AppDbContext db, CancellationToken ct)
    {
        var total = await db.MetaAttributes.CountAsync(ct);

        var entities = await db.MetaAttributes
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .Skip(request.Offset)
            .Take(request.Limit)
            .ToListAsync(ct);

        var items = entities.Select(CreateMetaAttributeEndpoint.ToResponse).ToList();

        return TypedResults.Ok(new PageResponse<CreateMetaAttributeEndpoint.Response> { Items = items, Count = total });
    }
}
