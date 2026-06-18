using SQLModule.Domain.Training;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.Training.Topics;

public sealed class GetAllTopicsEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Training.Topics.Collection, Handle)
            .WithName("GetAllTopics")
            .WithTags("Training")
            .Produces<PageResponse<CreateTopicEndpoint.Response>>()
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

    private static async Task<Ok<PageResponse<CreateTopicEndpoint.Response>>> Handle(
        [AsParameters] Request request, TemplateDbContext db, CancellationToken ct)
    {
        var total = await db.Topics.CountAsync(ct);

        var entities = await db.Topics
            .AsNoTracking()
            .OrderBy(x => x.TopicName)
            .Skip(request.Offset)
            .Take(request.Limit)
            .ToListAsync(ct);

        var items = entities.Select(CreateTopicEndpoint.ToResponse).ToList();

        return TypedResults.Ok(new PageResponse<CreateTopicEndpoint.Response> { Items = items, Count = total });
    }
}
