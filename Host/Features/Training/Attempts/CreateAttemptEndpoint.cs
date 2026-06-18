using SQLModule.Domain.Common;
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

public sealed class CreateAttemptEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Training.Attempts.Collection, Handle)
            .WithName("CreateAttempt")
            .WithTags("Training")
            .Produces<Response>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<Request>>();
    }

    public record Request(
        bool IsSuccess,
        DateTimeOffset StartAttempt,
        DateTimeOffset EndAttempt,
        Guid TaskId,
        Guid QueryId);

    public record Response(
        Guid Id,
        Guid UserId,
        bool IsSuccess,
        DateTimeOffset StartAttempt,
        DateTimeOffset EndAttempt,
        Guid TaskId,
        Guid QueryId,
        Guid CreatedById,
        DateTimeOffset CreatedAt,
        Guid UpdatedById,
        DateTimeOffset UpdatedAt);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.TaskId).NotEmpty();
            RuleFor(x => x.QueryId).NotEmpty();
            RuleFor(x => x.EndAttempt).GreaterThanOrEqualTo(x => x.StartAttempt);
        }
    }

    private static async Task<Created<Response>> Handle(
        Request request, AppDbContext db, ICurrentUser currentUser, CancellationToken ct)
    {
        if (!await db.SqlTasks.AnyAsync(x => x.Id == request.TaskId, ct))
        {
            throw new NotFoundException(nameof(SqlTask), request.TaskId);
        }

        if (!await db.SqlQueries.AnyAsync(x => x.Id == request.QueryId, ct))
        {
            throw new NotFoundException(nameof(SqlQuery), request.QueryId);
        }

        var userId = currentUser.UserId ?? SystemUser.Id;

        var entity = Attempt.Create(
            userId, request.IsSuccess, request.StartAttempt, request.EndAttempt,
            request.TaskId, request.QueryId);

        db.Attempts.Add(entity);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created(ApiRoutes.Training.Attempts.ForId(entity.Id), ToResponse(entity));
    }

    internal static Response ToResponse(Attempt e) => new(
        e.Id, e.UserId, e.IsSuccess, e.StartAttempt, e.EndAttempt, e.TaskId, e.QueryId,
        e.CreatedById, e.CreatedAt, e.UpdatedById, e.UpdatedAt);
}
