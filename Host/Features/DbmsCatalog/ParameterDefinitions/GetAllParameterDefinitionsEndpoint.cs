using SQLModule.Domain.DbmsCatalog;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.DbmsCatalog.ParameterDefinitions;

public sealed class GetAllParameterDefinitionsEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.DbmsCatalog.ParameterDefinitions.Collection, Handle)
            .WithName("GetAllParameterDefinitions")
            .WithTags("DbmsCatalog")
            .Produces<PageResponse<CreateParameterDefinitionEndpoint.Response>>()
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

    private static async Task<Ok<PageResponse<CreateParameterDefinitionEndpoint.Response>>> Handle(
        [AsParameters] Request request, AppDbContext db, CancellationToken ct)
    {
        var total = await db.ParameterDefinitions.CountAsync(ct);

        var entities = await db.ParameterDefinitions
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .Skip(request.Offset)
            .Take(request.Limit)
            .ToListAsync(ct);

        var items = entities.Select(CreateParameterDefinitionEndpoint.ToResponse).ToList();

        return TypedResults.Ok(new PageResponse<CreateParameterDefinitionEndpoint.Response> { Items = items, Count = total });
    }
}
