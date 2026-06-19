using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Domain;
using SQLModule.Domain.Exceptions;
using SQLModule.Domain.Schema;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.Schema.MetaAttributes;

public sealed class UpdateMetaAttributeEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiRoutes.Schema.MetaAttributes.ById, Handle)
            .WithName("UpdateMetaAttribute")
            .WithTags("Schema")
            .Produces<CreateMetaAttributeEndpoint.Response>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddEndpointFilter<ValidationFilter<Request>>();
    }

    public record Request(string AttributeName, bool IsPrimaryKey, bool IsRequired, short SortOrder);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.AttributeName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.SortOrder).GreaterThanOrEqualTo((short)0);
        }
    }

    private static async Task<Ok<CreateMetaAttributeEndpoint.Response>> Handle(
        Guid id, Request request, AppDbContext db, CancellationToken ct)
    {
        var entity = await db.MetaAttributes
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(MetaAttribute), id);

        entity.Update(request.AttributeName, request.IsPrimaryKey, request.IsRequired, request.SortOrder);
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(CreateMetaAttributeEndpoint.ToResponse(entity));
    }
}
