using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Domain.DbmsCatalog;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.DbmsCatalog.PhysicalTypes;

public sealed class GetAllPhysicalTypesEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.DbmsCatalog.PhysicalTypes.Collection, Handle)
            .WithName("GetAllPhysicalTypes")
            .WithTags("DbmsCatalog")
            .Produces<PageResponse<CreatePhysicalTypeEndpoint.Response>>()
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

    private static async Task<Ok<PageResponse<CreatePhysicalTypeEndpoint.Response>>> Handle(
        [AsParameters] Request request, AppDbContext db, CancellationToken ct)
    {
        var total = await db.PhysicalTypes.CountAsync(ct);

        var entities = await db.PhysicalTypes
            .AsNoTracking()
            .OrderBy(x => x.TypeName)
            .Skip(request.Offset)
            .Take(request.Limit)
            .ToListAsync(ct);

        var items = entities.Select(CreatePhysicalTypeEndpoint.ToResponse).ToList();

        return TypedResults.Ok(new PageResponse<CreatePhysicalTypeEndpoint.Response> { Items = items, Count = total });
    }
}
