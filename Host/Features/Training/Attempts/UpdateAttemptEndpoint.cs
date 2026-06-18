using SQLModule.Domain.Training;
using SQLModule.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Domain;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.Training.Attempts;

public sealed class UpdateAttemptEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiRoutes.Training.Attempts.ById, Handle)
            .WithName("UpdateAttempt")
            .WithTags("Training")
            .Produces<CreateAttemptEndpoint.Response>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddEndpointFilter<ValidationFilter<Request>>();
    }

    public record Request(bool IsSuccess, DateTimeOffset EndAttempt);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.EndAttempt).NotEmpty();
        }
    }

    private static async Task<Ok<CreateAttemptEndpoint.Response>> Handle(
        Guid id, Request request, TemplateDbContext db, CancellationToken ct)
    {
        var entity = await db.Attempts
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(Attempt), id);

        entity.Update(request.IsSuccess, request.EndAttempt);
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(CreateAttemptEndpoint.ToResponse(entity));
    }
}
