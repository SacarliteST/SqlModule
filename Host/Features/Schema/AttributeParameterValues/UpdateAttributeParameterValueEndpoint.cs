using SQLModule.Domain.Schema;
using SQLModule.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Domain;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.Schema.AttributeParameterValues;

public sealed class UpdateAttributeParameterValueEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiRoutes.Schema.AttributeParameterValues.ById, Handle)
            .WithName("UpdateAttributeParameterValue")
            .WithTags("Schema")
            .Produces<CreateAttributeParameterValueEndpoint.Response>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddEndpointFilter<ValidationFilter<Request>>();
    }

    public record Request(string ParameterValue);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.ParameterValue).NotEmpty().MaximumLength(500);
        }
    }

    private static async Task<Ok<CreateAttributeParameterValueEndpoint.Response>> Handle(
        Guid id, Request request, TemplateDbContext db, CancellationToken ct)
    {
        var entity = await db.AttributeParameterValues
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(AttributeParameterValue), id);

        entity.Update(request.ParameterValue);
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(CreateAttributeParameterValueEndpoint.ToResponse(entity));
    }
}
