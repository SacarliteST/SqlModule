using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Domain;
using SQLModule.Domain.DbmsCatalog;
using SQLModule.Domain.Exceptions;
using SQLModule.Domain.Schema;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.Schema.AttributeParameterValues;

public sealed class CreateAttributeParameterValueEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Schema.AttributeParameterValues.Collection, Handle)
            .WithName("CreateAttributeParameterValue")
            .WithTags("Schema")
            .Produces<Response>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<Request>>();
    }

    public record Request(Guid MetaAttributeId, Guid ParameterDefinitionId, string ParameterValue);

    public record Response(
        Guid Id,
        Guid MetaAttributeId,
        Guid ParameterDefinitionId,
        string ParameterValue,
        Guid CreatedById,
        DateTimeOffset CreatedAt,
        Guid UpdatedById,
        DateTimeOffset UpdatedAt);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.MetaAttributeId).NotEmpty();
            RuleFor(x => x.ParameterDefinitionId).NotEmpty();
            RuleFor(x => x.ParameterValue).NotEmpty().MaximumLength(500);
        }
    }

    private static async Task<Created<Response>> Handle(
        Request request, AppDbContext db, CancellationToken ct)
    {
        if (!await db.MetaAttributes.AnyAsync(x => x.Id == request.MetaAttributeId, ct))
        {
            throw new NotFoundException(nameof(MetaAttribute), request.MetaAttributeId);
        }

        if (!await db.ParameterDefinitions.AnyAsync(x => x.Id == request.ParameterDefinitionId, ct))
        {
            throw new NotFoundException(nameof(ParameterDefinition), request.ParameterDefinitionId);
        }

        var entity = AttributeParameterValue.Create(
            request.MetaAttributeId, request.ParameterDefinitionId, request.ParameterValue);

        db.AttributeParameterValues.Add(entity);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created(
            ApiRoutes.Schema.AttributeParameterValues.ForId(entity.Id), ToResponse(entity));
    }

    internal static Response ToResponse(AttributeParameterValue e) => new(
        e.Id, e.MetaAttributeId, e.ParameterDefinitionId, e.ParameterValue,
        e.CreatedById, e.CreatedAt, e.UpdatedById, e.UpdatedAt);
}
