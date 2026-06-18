using SQLModule.Domain.Training;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.Training.Attempts;

public sealed class GetAllAttemptsEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Training.Attempts.Collection, Handle)
            .WithName("GetAllAttempts")
            .WithTags("Training")
            .Produces<PageResponse<CreateAttemptEndpoint.Response>>()
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

    private static async Task<Ok<PageResponse<CreateAttemptEndpoint.Response>>> Handle(
        [AsParameters] Request request, AppDbContext db, CancellationToken ct)
    {
        var total = await db.Attempts.CountAsync(ct);

        var entities = await db.Attempts
            .AsNoTracking()
            .OrderByDescending(x => x.StartAttempt)
            .Skip(request.Offset)
            .Take(request.Limit)
            .ToListAsync(ct);

        var items = entities.Select(CreateAttemptEndpoint.ToResponse).ToList();

        return TypedResults.Ok(new PageResponse<CreateAttemptEndpoint.Response> { Items = items, Count = total });
    }
}
