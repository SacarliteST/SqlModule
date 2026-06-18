using SQLModule.Domain.Training;
using SQLModule.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Domain;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.Training.SqlQueries;

public sealed class UpdateSqlQueryEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiRoutes.Training.SqlQueries.ById, Handle)
            .WithName("UpdateSqlQuery")
            .WithTags("Training")
            .Produces<CreateSqlQueryEndpoint.Response>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddEndpointFilter<ValidationFilter<Request>>();
    }

    public record Request(string QueryText, bool StrictColumnOrder, bool StrictRowOrder);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.QueryText).NotEmpty();
        }
    }

    private static async Task<Ok<CreateSqlQueryEndpoint.Response>> Handle(
        Guid id, Request request, TemplateDbContext db, CancellationToken ct)
    {
        var entity = await db.SqlQueries
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(SqlQuery), id);

        entity.Update(request.QueryText, request.StrictColumnOrder, request.StrictRowOrder);
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(CreateSqlQueryEndpoint.ToResponse(entity));
    }
}
