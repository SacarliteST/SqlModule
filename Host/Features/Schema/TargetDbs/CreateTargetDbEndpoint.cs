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

namespace SQLModule.Host.Features.Schema.TargetDbs;

public sealed class CreateTargetDbEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Schema.TargetDbs.Collection, Handle)
            .WithName("CreateTargetDb")
            .WithTags("Schema")
            .Produces<Response>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<Request>>();
    }

    public record Request(Guid DbmsId, string DbName, string? Description, bool IsReadOnly);

    public record Response(
        Guid Id,
        Guid DbmsId,
        string DbName,
        string? Description,
        bool IsReadOnly,
        Guid CreatedById,
        DateTimeOffset CreatedAt,
        Guid UpdatedById,
        DateTimeOffset UpdatedAt);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.DbmsId).NotEmpty();
            RuleFor(x => x.DbName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(500);
        }
    }

    private static async Task<Created<Response>> Handle(
        Request request, TemplateDbContext db, CancellationToken ct)
    {
        if (!await db.DbmsDictionaries.AnyAsync(x => x.Id == request.DbmsId, ct))
        {
            throw new NotFoundException(nameof(DbmsDictionary), request.DbmsId);
        }

        var entity = TargetDb.Create(request.DbmsId, request.DbName, request.Description, request.IsReadOnly);
        db.TargetDbs.Add(entity);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created(ApiRoutes.Schema.TargetDbs.ForId(entity.Id), ToResponse(entity));
    }

    internal static Response ToResponse(TargetDb e) => new(
        e.Id, e.DbmsId, e.DbName, e.Description, e.IsReadOnly,
        e.CreatedById, e.CreatedAt, e.UpdatedById, e.UpdatedAt);
}
