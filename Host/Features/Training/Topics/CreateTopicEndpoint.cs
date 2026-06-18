using SQLModule.Domain.Training;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Domain;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.Training.Topics;

public sealed class CreateTopicEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Training.Topics.Collection, Handle)
            .WithName("CreateTopic")
            .WithTags("Training")
            .Produces<Response>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<Request>>();
    }

    public record Request(string TopicName, Guid? ParentTopicId);

    public record Response(
        Guid Id,
        string TopicName,
        Guid? ParentTopicId,
        Guid CreatedById,
        DateTimeOffset CreatedAt,
        Guid UpdatedById,
        DateTimeOffset UpdatedAt);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.TopicName).NotEmpty().MaximumLength(300);
        }
    }

    private static async Task<Created<Response>> Handle(
        Request request, TemplateDbContext db, CancellationToken ct)
    {
        var entity = Topic.Create(request.TopicName, request.ParentTopicId);
        db.Topics.Add(entity);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created(ApiRoutes.Training.Topics.ForId(entity.Id), ToResponse(entity));
    }

    internal static Response ToResponse(Topic e) => new(
        e.Id, e.TopicName, e.ParentTopicId,
        e.CreatedById, e.CreatedAt, e.UpdatedById, e.UpdatedAt);
}
