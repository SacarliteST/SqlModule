using SQLModule.Domain.Schema;
using SQLModule.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Domain;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.Schema.MetaTables;

public sealed class UpdateMetaTableEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiRoutes.Schema.MetaTables.ById, Handle)
            .WithName("UpdateMetaTable")
            .WithTags("Schema")
            .Produces<CreateMetaTableEndpoint.Response>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddEndpointFilter<ValidationFilter<Request>>();
    }

    public record Request(string TableName, string? Description);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.TableName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(500);
        }
    }

    private static async Task<Ok<CreateMetaTableEndpoint.Response>> Handle(
        Guid id, Request request, AppDbContext db, CancellationToken ct)
    {
        var entity = await db.MetaTables
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(MetaTable), id);

        entity.Update(request.TableName, request.Description);
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(CreateMetaTableEndpoint.ToResponse(entity));
    }
}
