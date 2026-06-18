using SQLModule.Domain.Training;
using SQLModule.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Domain;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.Training.SqlTasks;

public sealed class UpdateSqlTaskEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiRoutes.Training.SqlTasks.ById, Handle)
            .WithName("UpdateSqlTask")
            .WithTags("Training")
            .Produces<CreateSqlTaskEndpoint.Response>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddEndpointFilter<ValidationFilter<Request>>();
    }

    public record Request(string TaskName, string TaskText, short DifficultyLevel);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.TaskName).NotEmpty().MaximumLength(300);
            RuleFor(x => x.TaskText).NotEmpty();
            RuleFor(x => x.DifficultyLevel).InclusiveBetween((short)1, (short)5);
        }
    }

    private static async Task<Ok<CreateSqlTaskEndpoint.Response>> Handle(
        Guid id, Request request, AppDbContext db, CancellationToken ct)
    {
        var entity = await db.SqlTasks
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(SqlTask), id);

        entity.Update(request.TaskName, request.TaskText, request.DifficultyLevel);
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(CreateSqlTaskEndpoint.ToResponse(entity));
    }
}
