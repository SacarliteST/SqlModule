using SQLModule.Domain.Training;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Domain;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.Training.SqlQueries;

public sealed class CreateSqlQueryEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Training.SqlQueries.Collection, Handle)
            .WithName("CreateSqlQuery")
            .WithTags("Training")
            .Produces<Response>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<Request>>();
    }

    public record Request(string QueryText, bool StrictColumnOrder, bool StrictRowOrder);

    public record Response(
        Guid Id,
        string QueryText,
        bool StrictColumnOrder,
        bool StrictRowOrder,
        Guid CreatedById,
        DateTimeOffset CreatedAt,
        Guid UpdatedById,
        DateTimeOffset UpdatedAt);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.QueryText).NotEmpty();
        }
    }

    private static async Task<Created<Response>> Handle(
        Request request, TemplateDbContext db, CancellationToken ct)
    {
        var entity = SqlQuery.Create(request.QueryText, request.StrictColumnOrder, request.StrictRowOrder);
        db.SqlQueries.Add(entity);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created(ApiRoutes.Training.SqlQueries.ForId(entity.Id), ToResponse(entity));
    }

    internal static Response ToResponse(SqlQuery e) => new(
        e.Id, e.QueryText, e.StrictColumnOrder, e.StrictRowOrder,
        e.CreatedById, e.CreatedAt, e.UpdatedById, e.UpdatedAt);
}
