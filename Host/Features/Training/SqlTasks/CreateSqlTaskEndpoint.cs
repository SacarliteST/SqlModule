using SQLModule.Domain.Schema;
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

public sealed class CreateSqlTaskEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Training.SqlTasks.Collection, Handle)
            .WithName("CreateSqlTask")
            .WithTags("Training")
            .Produces<Response>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<Request>>();
    }

    public record Request(
        Guid TargetDbId,
        Guid TopicId,
        Guid SqlQueryId,
        string TaskName,
        string TaskText,
        short DifficultyLevel);

    public record Response(
        Guid Id,
        Guid TargetDbId,
        Guid TopicId,
        Guid SqlQueryId,
        string TaskName,
        string TaskText,
        short DifficultyLevel,
        Guid CreatedById,
        DateTimeOffset CreatedAt,
        Guid UpdatedById,
        DateTimeOffset UpdatedAt);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.TargetDbId).NotEmpty();
            RuleFor(x => x.TopicId).NotEmpty();
            RuleFor(x => x.SqlQueryId).NotEmpty();
            RuleFor(x => x.TaskName).NotEmpty().MaximumLength(300);
            RuleFor(x => x.TaskText).NotEmpty();
            RuleFor(x => x.DifficultyLevel).InclusiveBetween((short)1, (short)5);
        }
    }

    private static async Task<Created<Response>> Handle(
        Request request, AppDbContext db, CancellationToken ct)
    {
        if (!await db.TargetDbs.AnyAsync(x => x.Id == request.TargetDbId, ct))
        {
            throw new NotFoundException(nameof(TargetDb), request.TargetDbId);
        }

        if (!await db.Topics.AnyAsync(x => x.Id == request.TopicId, ct))
        {
            throw new NotFoundException(nameof(Topic), request.TopicId);
        }

        if (!await db.SqlQueries.AnyAsync(x => x.Id == request.SqlQueryId, ct))
        {
            throw new NotFoundException(nameof(SqlQuery), request.SqlQueryId);
        }

        var entity = SqlTask.Create(
            request.TargetDbId, request.TopicId, request.SqlQueryId,
            request.TaskName, request.TaskText, request.DifficultyLevel);

        db.SqlTasks.Add(entity);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created(ApiRoutes.Training.SqlTasks.ForId(entity.Id), ToResponse(entity));
    }

    internal static Response ToResponse(SqlTask e) => new(
        e.Id, e.TargetDbId, e.TopicId, e.SqlQueryId,
        e.TaskName, e.TaskText, e.DifficultyLevel,
        e.CreatedById, e.CreatedAt, e.UpdatedById, e.UpdatedAt);
}
