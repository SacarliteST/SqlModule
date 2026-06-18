using SQLModule.Domain.Schema;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.Schema.TargetDbs;

public sealed class GetAllTargetDbsEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Schema.TargetDbs.Collection, Handle)
            .WithName("GetAllTargetDbs")
            .WithTags("Schema")
            .Produces<PageResponse<CreateTargetDbEndpoint.Response>>()
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

    private static async Task<Ok<PageResponse<CreateTargetDbEndpoint.Response>>> Handle(
        [AsParameters] Request request, TemplateDbContext db, CancellationToken ct)
    {
        var total = await db.TargetDbs.CountAsync(ct);

        var entities = await db.TargetDbs
            .AsNoTracking()
            .OrderBy(x => x.DbName)
            .Skip(request.Offset)
            .Take(request.Limit)
            .ToListAsync(ct);

        var items = entities.Select(CreateTargetDbEndpoint.ToResponse).ToList();

        return TypedResults.Ok(new PageResponse<CreateTargetDbEndpoint.Response> { Items = items, Count = total });
    }
}
