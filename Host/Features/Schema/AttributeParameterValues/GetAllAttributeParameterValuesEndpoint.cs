using SQLModule.Domain.Schema;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.Schema.AttributeParameterValues;

public sealed class GetAllAttributeParameterValuesEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Schema.AttributeParameterValues.Collection, Handle)
            .WithName("GetAllAttributeParameterValues")
            .WithTags("Schema")
            .Produces<PageResponse<CreateAttributeParameterValueEndpoint.Response>>()
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

    private static async Task<Ok<PageResponse<CreateAttributeParameterValueEndpoint.Response>>> Handle(
        [AsParameters] Request request, TemplateDbContext db, CancellationToken ct)
    {
        var total = await db.AttributeParameterValues.CountAsync(ct);

        var entities = await db.AttributeParameterValues
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Skip(request.Offset)
            .Take(request.Limit)
            .ToListAsync(ct);

        var items = entities.Select(CreateAttributeParameterValueEndpoint.ToResponse).ToList();

        return TypedResults.Ok(new PageResponse<CreateAttributeParameterValueEndpoint.Response> { Items = items, Count = total });
    }
}
