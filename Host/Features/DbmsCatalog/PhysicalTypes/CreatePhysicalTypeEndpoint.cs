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

public sealed class CreatePhysicalTypeEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.DbmsCatalog.PhysicalTypes.Collection, Handle)
            .WithName("CreatePhysicalType")
            .WithTags("DbmsCatalog")
            .Produces<Response>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<Request>>();
    }

    public record Request(Guid DbmsId, string TypeName);

    public record Response(
        Guid Id,
        Guid DbmsId,
        string TypeName,
        Guid CreatedById,
        DateTimeOffset CreatedAt,
        Guid UpdatedById,
        DateTimeOffset UpdatedAt);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.DbmsId).NotEmpty();
            RuleFor(x => x.TypeName).NotEmpty().MaximumLength(200);
        }
    }

    private static async Task<Created<Response>> Handle(
        Request request, AppDbContext db, CancellationToken ct)
    {
        if (!await db.DbmsDictionaries.AnyAsync(x => x.Id == request.DbmsId, ct))
        {
            throw new NotFoundException(nameof(DbmsDictionary), request.DbmsId);
        }

        var entity = PhysicalType.Create(request.DbmsId, request.TypeName);
        db.PhysicalTypes.Add(entity);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created(ApiRoutes.DbmsCatalog.PhysicalTypes.ForId(entity.Id), ToResponse(entity));
    }

    internal static Response ToResponse(PhysicalType e) => new(
        e.Id, e.DbmsId, e.TypeName,
        e.CreatedById, e.CreatedAt, e.UpdatedById, e.UpdatedAt);
}
