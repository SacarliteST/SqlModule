using SQLModule.Domain.Schema;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.Schema.MetaTables;

public sealed class GetAllMetaTablesEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Schema.MetaTables.Collection, Handle)
            .WithName("GetAllMetaTables")
            .WithTags("Schema")
            .Produces<PageResponse<CreateMetaTableEndpoint.Response>>()
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

    private static async Task<Ok<PageResponse<CreateMetaTableEndpoint.Response>>> Handle(
        [AsParameters] Request request, TemplateDbContext db, CancellationToken ct)
    {
        var total = await db.MetaTables.CountAsync(ct);

        var entities = await db.MetaTables
            .AsNoTracking()
            .OrderBy(x => x.TableName)
            .Skip(request.Offset)
            .Take(request.Limit)
            .ToListAsync(ct);

        var items = entities.Select(CreateMetaTableEndpoint.ToResponse).ToList();

        return TypedResults.Ok(new PageResponse<CreateMetaTableEndpoint.Response> { Items = items, Count = total });
    }
}
