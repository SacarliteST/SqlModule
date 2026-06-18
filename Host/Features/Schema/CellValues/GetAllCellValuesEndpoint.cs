using SQLModule.Domain.Schema;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.Schema.CellValues;

public sealed class GetAllCellValuesEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Schema.CellValues.Collection, Handle)
            .WithName("GetAllCellValues")
            .WithTags("Schema")
            .Produces<PageResponse<CreateCellValueEndpoint.Response>>()
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

    private static async Task<Ok<PageResponse<CreateCellValueEndpoint.Response>>> Handle(
        [AsParameters] Request request, AppDbContext db, CancellationToken ct)
    {
        var total = await db.CellValues.CountAsync(ct);

        var entities = await db.CellValues
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Skip(request.Offset)
            .Take(request.Limit)
            .ToListAsync(ct);

        var items = entities.Select(CreateCellValueEndpoint.ToResponse).ToList();

        return TypedResults.Ok(new PageResponse<CreateCellValueEndpoint.Response> { Items = items, Count = total });
    }
}
