using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Domain;
using SQLModule.Domain.Exceptions;
using SQLModule.Domain.Schema;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.Schema.MetaRelationships;

public sealed class CreateMetaRelationshipEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Schema.MetaRelationships.Collection, Handle)
            .WithName("CreateMetaRelationship")
            .WithTags("Schema")
            .Produces<Response>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<Request>>();
    }

    public record Request(
        string RelationshipName,
        Guid SourceAttributeId,
        Guid TargetAttributeId,
        string? DeleteRule,
        string? UpdateRule);

    public record Response(
        Guid Id,
        string RelationshipName,
        Guid SourceAttributeId,
        Guid TargetAttributeId,
        string? DeleteRule,
        string? UpdateRule,
        Guid CreatedById,
        DateTimeOffset CreatedAt,
        Guid UpdatedById,
        DateTimeOffset UpdatedAt);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.RelationshipName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.SourceAttributeId).NotEmpty();
            RuleFor(x => x.TargetAttributeId).NotEmpty();
            RuleFor(x => x.DeleteRule).MaximumLength(50);
            RuleFor(x => x.UpdateRule).MaximumLength(50);
        }
    }

    private static async Task<Created<Response>> Handle(
        Request request, AppDbContext db, CancellationToken ct)
    {
        if (!await db.MetaAttributes.AnyAsync(x => x.Id == request.SourceAttributeId, ct))
        {
            throw new NotFoundException(nameof(MetaAttribute), request.SourceAttributeId);
        }

        if (!await db.MetaAttributes.AnyAsync(x => x.Id == request.TargetAttributeId, ct))
        {
            throw new NotFoundException(nameof(MetaAttribute), request.TargetAttributeId);
        }

        var entity = MetaRelationship.Create(
            request.RelationshipName, request.SourceAttributeId, request.TargetAttributeId,
            request.DeleteRule, request.UpdateRule);

        db.MetaRelationships.Add(entity);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created(ApiRoutes.Schema.MetaRelationships.ForId(entity.Id), ToResponse(entity));
    }

    internal static Response ToResponse(MetaRelationship e) => new(
        e.Id, e.RelationshipName, e.SourceAttributeId, e.TargetAttributeId,
        e.DeleteRule, e.UpdateRule,
        e.CreatedById, e.CreatedAt, e.UpdatedById, e.UpdatedAt);
}
