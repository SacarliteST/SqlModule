using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Domain.DbmsCatalog;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.DbmsCatalog.DbmsDictionary;

public sealed class GetAllDbmsDictionariesEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.DbmsCatalog.DbmsDictionaries.Collection, Handle)
            .WithName("GetAllDbmsDictionaries")
            .WithTags("DbmsCatalog")
            .Produces<PageResponse<CreateDbmsDictionaryEndpoint.Response>>()
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

    private static async Task<Ok<PageResponse<CreateDbmsDictionaryEndpoint.Response>>> Handle(
        [AsParameters] Request request, AppDbContext db, CancellationToken ct)
    {
        var total = await db.DbmsDictionaries.CountAsync(ct);

        var entities = await db.DbmsDictionaries
            .AsNoTracking()
            .OrderBy(x => x.DbmsName)
            .Skip(request.Offset)
            .Take(request.Limit)
            .ToListAsync(ct);

        var items = entities.Select(CreateDbmsDictionaryEndpoint.ToResponse).ToList();

        return TypedResults.Ok(new PageResponse<CreateDbmsDictionaryEndpoint.Response> { Items = items, Count = total });
    }
}
