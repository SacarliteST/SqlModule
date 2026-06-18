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

public sealed class CreateMetaTableEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Schema.MetaTables.Collection, Handle)
            .WithName("CreateMetaTable")
            .WithTags("Schema")
            .Produces<Response>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<Request>>();
    }

    public record Request(Guid TargetDbId, string TableName, string? Description);

    public record Response(
        Guid Id,
        Guid TargetDbId,
        string TableName,
        string? Description,
        Guid CreatedById,
        DateTimeOffset CreatedAt,
        Guid UpdatedById,
        DateTimeOffset UpdatedAt);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.TargetDbId).NotEmpty();
            RuleFor(x => x.TableName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(500);
        }
    }

    private static async Task<Created<Response>> Handle(
        Request request, TemplateDbContext db, CancellationToken ct)
    {
        if (!await db.TargetDbs.AnyAsync(x => x.Id == request.TargetDbId, ct))
        {
            throw new NotFoundException(nameof(TargetDb), request.TargetDbId);
        }

        var entity = MetaTable.Create(request.TargetDbId, request.TableName, request.Description);
        db.MetaTables.Add(entity);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created(ApiRoutes.Schema.MetaTables.ForId(entity.Id), ToResponse(entity));
    }

    internal static Response ToResponse(MetaTable e) => new(
        e.Id, e.TargetDbId, e.TableName, e.Description,
        e.CreatedById, e.CreatedAt, e.UpdatedById, e.UpdatedAt);
}
