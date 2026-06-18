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

public sealed class UpdateParameterDefinitionEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiRoutes.DbmsCatalog.ParameterDefinitions.ById, Handle)
            .WithName("UpdateParameterDefinition")
            .WithTags("DbmsCatalog")
            .Produces<CreateParameterDefinitionEndpoint.Response>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddEndpointFilter<ValidationFilter<Request>>();
    }

    public record Request(
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

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
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

    private static async Task<Ok<CreateParameterDefinitionEndpoint.Response>> Handle(
        Guid id, Request request, TemplateDbContext db, CancellationToken ct)
    {
        var entity = await db.ParameterDefinitions
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(ParameterDefinition), id);

        entity.Update(
            request.ParameterKey, request.DisplayName, request.InputType,
            request.DefaultValue, request.SortOrder, request.SqlFragment, request.IsRequired,
            request.ValuePrefix, request.ValueSuffix, request.Separator);

        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(CreateParameterDefinitionEndpoint.ToResponse(entity));
    }
}
