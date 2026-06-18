using SQLModule.Domain.DbmsCatalog;
using SQLModule.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Domain;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.DbmsCatalog.PhysicalTypes;

public sealed class UpdatePhysicalTypeEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiRoutes.DbmsCatalog.PhysicalTypes.ById, Handle)
            .WithName("UpdatePhysicalType")
            .WithTags("DbmsCatalog")
            .Produces<CreatePhysicalTypeEndpoint.Response>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddEndpointFilter<ValidationFilter<Request>>();
    }

    public record Request(string TypeName);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.TypeName).NotEmpty().MaximumLength(200);
        }
    }

    private static async Task<Ok<CreatePhysicalTypeEndpoint.Response>> Handle(
        Guid id, Request request, AppDbContext db, CancellationToken ct)
    {
        var entity = await db.PhysicalTypes
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(PhysicalType), id);

        entity.Update(request.TypeName);
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(CreatePhysicalTypeEndpoint.ToResponse(entity));
    }
}
