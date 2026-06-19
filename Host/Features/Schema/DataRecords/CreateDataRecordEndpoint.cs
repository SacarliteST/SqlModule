using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Domain;
using SQLModule.Domain.Exceptions;
using SQLModule.Domain.Schema;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.Schema.DataRecords;

public sealed class CreateDataRecordEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Schema.DataRecords.Collection, Handle)
            .WithName("CreateDataRecord")
            .WithTags("Schema")
            .Produces<Response>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<Request>>();
    }

    public record Request(Guid MetaTableId, int? SortOrder);

    public record Response(
        Guid Id,
        Guid MetaTableId,
        int? SortOrder,
        Guid CreatedById,
        DateTimeOffset CreatedAt,
        Guid UpdatedById,
        DateTimeOffset UpdatedAt);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.MetaTableId).NotEmpty();
        }
    }

    private static async Task<Created<Response>> Handle(
        Request request, AppDbContext db, CancellationToken ct)
    {
        if (!await db.MetaTables.AnyAsync(x => x.Id == request.MetaTableId, ct))
        {
            throw new NotFoundException(nameof(MetaTable), request.MetaTableId);
        }

        var entity = DataRecord.Create(request.MetaTableId, request.SortOrder);
        db.DataRecords.Add(entity);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created(ApiRoutes.Schema.DataRecords.ForId(entity.Id), ToResponse(entity));
    }

    internal static Response ToResponse(DataRecord e) => new(
        e.Id, e.MetaTableId, e.SortOrder,
        e.CreatedById, e.CreatedAt, e.UpdatedById, e.UpdatedAt);
}
