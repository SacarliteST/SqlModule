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

public sealed class UpdateMetaRelationshipEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiRoutes.Schema.MetaRelationships.ById, Handle)
            .WithName("UpdateMetaRelationship")
            .WithTags("Schema")
            .Produces<CreateMetaRelationshipEndpoint.Response>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddEndpointFilter<ValidationFilter<Request>>();
    }

    public record Request(string RelationshipName, string? DeleteRule, string? UpdateRule);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.RelationshipName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.DeleteRule).MaximumLength(50);
            RuleFor(x => x.UpdateRule).MaximumLength(50);
        }
    }

    private static async Task<Ok<CreateMetaRelationshipEndpoint.Response>> Handle(
        Guid id, Request request, AppDbContext db, CancellationToken ct)
    {
        var entity = await db.MetaRelationships
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(MetaRelationship), id);

        entity.Update(request.RelationshipName, request.DeleteRule, request.UpdateRule);
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(CreateMetaRelationshipEndpoint.ToResponse(entity));
    }
}
