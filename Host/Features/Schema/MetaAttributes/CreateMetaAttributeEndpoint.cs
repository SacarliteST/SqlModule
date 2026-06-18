using SQLModule.Domain.DbmsCatalog;
using SQLModule.Domain.Schema;
using SQLModule.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Domain;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.Schema.MetaAttributes;

public sealed class CreateMetaAttributeEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Schema.MetaAttributes.Collection, Handle)
            .WithName("CreateMetaAttribute")
            .WithTags("Schema")
            .Produces<Response>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<Request>>();
    }

    public record Request(
        Guid MetaTableId,
        Guid PhysicalTypeId,
        string AttributeName,
        bool IsPrimaryKey,
        bool IsRequired,
        short SortOrder);

    public record Response(
        Guid Id,
        Guid MetaTableId,
        Guid PhysicalTypeId,
        string AttributeName,
        bool IsPrimaryKey,
        bool IsRequired,
        short SortOrder,
        Guid CreatedById,
        DateTimeOffset CreatedAt,
        Guid UpdatedById,
        DateTimeOffset UpdatedAt);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.MetaTableId).NotEmpty();
            RuleFor(x => x.PhysicalTypeId).NotEmpty();
            RuleFor(x => x.AttributeName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.SortOrder).GreaterThanOrEqualTo((short)0);
        }
    }

    private static async Task<Created<Response>> Handle(
        Request request, TemplateDbContext db, CancellationToken ct)
    {
        if (!await db.MetaTables.AnyAsync(x => x.Id == request.MetaTableId, ct))
        {
            throw new NotFoundException(nameof(MetaTable), request.MetaTableId);
        }

        if (!await db.PhysicalTypes.AnyAsync(x => x.Id == request.PhysicalTypeId, ct))
        {
            throw new NotFoundException(nameof(PhysicalType), request.PhysicalTypeId);
        }

        var entity = MetaAttribute.Create(
            request.MetaTableId, request.PhysicalTypeId,
            request.AttributeName, request.IsPrimaryKey, request.IsRequired, request.SortOrder);

        db.MetaAttributes.Add(entity);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created(ApiRoutes.Schema.MetaAttributes.ForId(entity.Id), ToResponse(entity));
    }

    internal static Response ToResponse(MetaAttribute e) => new(
        e.Id, e.MetaTableId, e.PhysicalTypeId, e.AttributeName,
        e.IsPrimaryKey, e.IsRequired, e.SortOrder,
        e.CreatedById, e.CreatedAt, e.UpdatedById, e.UpdatedAt);
}
