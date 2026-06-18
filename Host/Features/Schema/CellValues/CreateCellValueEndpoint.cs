using SQLModule.Domain.Schema;
using SQLModule.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Domain;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.Schema.CellValues;

public sealed class CreateCellValueEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Schema.CellValues.Collection, Handle)
            .WithName("CreateCellValue")
            .WithTags("Schema")
            .Produces<Response>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<Request>>();
    }

    public record Request(Guid DataRecordId, Guid MetaAttributeId, string? TextValue);

    public record Response(
        Guid Id,
        Guid DataRecordId,
        Guid MetaAttributeId,
        string? TextValue,
        Guid CreatedById,
        DateTimeOffset CreatedAt,
        Guid UpdatedById,
        DateTimeOffset UpdatedAt);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.DataRecordId).NotEmpty();
            RuleFor(x => x.MetaAttributeId).NotEmpty();
            RuleFor(x => x.TextValue).MaximumLength(2000);
        }
    }

    private static async Task<Created<Response>> Handle(
        Request request, AppDbContext db, CancellationToken ct)
    {
        if (!await db.DataRecords.AnyAsync(x => x.Id == request.DataRecordId, ct))
        {
            throw new NotFoundException(nameof(DataRecord), request.DataRecordId);
        }

        if (!await db.MetaAttributes.AnyAsync(x => x.Id == request.MetaAttributeId, ct))
        {
            throw new NotFoundException(nameof(MetaAttribute), request.MetaAttributeId);
        }

        var entity = CellValue.Create(request.DataRecordId, request.MetaAttributeId, request.TextValue);
        db.CellValues.Add(entity);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created(ApiRoutes.Schema.CellValues.ForId(entity.Id), ToResponse(entity));
    }

    internal static Response ToResponse(CellValue e) => new(
        e.Id, e.DataRecordId, e.MetaAttributeId, e.TextValue,
        e.CreatedById, e.CreatedAt, e.UpdatedById, e.UpdatedAt);
}
