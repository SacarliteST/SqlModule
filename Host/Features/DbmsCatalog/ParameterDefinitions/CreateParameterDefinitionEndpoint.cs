using SQLModule.Domain.DbmsCatalog;
using SQLModule.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Domain;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.DbmsCatalog.ParameterDefinitions;

public sealed class CreateParameterDefinitionEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.DbmsCatalog.ParameterDefinitions.Collection, Handle)
            .WithName("CreateParameterDefinition")
            .WithTags("DbmsCatalog")
            .Produces<Response>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<Request>>();
    }

    public record Request(
        Guid PhysicalTypeId,
        string ParameterKey,
        string DisplayName,
        string InputType,
        string? DefaultValue,
        short SortOrder,
        string SqlFragment,
        bool IsRequired,
        string? ValuePrefix,
        string? ValueSuffix,
        string? Separator);

    public record Response(
        Guid Id,
        Guid PhysicalTypeId,
        string ParameterKey,
        string DisplayName,
        string InputType,
        string? DefaultValue,
        short SortOrder,
        string SqlFragment,
        bool IsRequired,
        string? ValuePrefix,
        string? ValueSuffix,
        string? Separator,
        Guid CreatedById,
        DateTimeOffset CreatedAt,
        Guid UpdatedById,
        DateTimeOffset UpdatedAt);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.PhysicalTypeId).NotEmpty();
            RuleFor(x => x.ParameterKey).NotEmpty().MaximumLength(100);
            RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.InputType).NotEmpty().MaximumLength(50);
            RuleFor(x => x.DefaultValue).MaximumLength(500);
            RuleFor(x => x.SortOrder).GreaterThanOrEqualTo((short)0);
            RuleFor(x => x.SqlFragment).NotEmpty().MaximumLength(500);
            RuleFor(x => x.ValuePrefix).MaximumLength(50);
            RuleFor(x => x.ValueSuffix).MaximumLength(50);
            RuleFor(x => x.Separator).MaximumLength(10);
        }
    }

    private static async Task<Created<Response>> Handle(
        Request request, TemplateDbContext db, CancellationToken ct)
    {
        if (!await db.PhysicalTypes.AnyAsync(x => x.Id == request.PhysicalTypeId, ct))
        {
            throw new NotFoundException(nameof(PhysicalType), request.PhysicalTypeId);
        }

        var entity = ParameterDefinition.Create(
            request.PhysicalTypeId, request.ParameterKey, request.DisplayName, request.InputType,
            request.DefaultValue, request.SortOrder, request.SqlFragment, request.IsRequired,
            request.ValuePrefix, request.ValueSuffix, request.Separator);

        db.ParameterDefinitions.Add(entity);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created(
            ApiRoutes.DbmsCatalog.ParameterDefinitions.ForId(entity.Id), ToResponse(entity));
    }

    internal static Response ToResponse(ParameterDefinition e) => new(
        e.Id, e.PhysicalTypeId, e.ParameterKey, e.DisplayName, e.InputType,
        e.DefaultValue, e.SortOrder, e.SqlFragment, e.IsRequired,
        e.ValuePrefix, e.ValueSuffix, e.Separator,
        e.CreatedById, e.CreatedAt, e.UpdatedById, e.UpdatedAt);
}
