using SQLModule.Domain.Training;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.Training.SqlTasks;

public sealed class GetAllSqlTasksEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Training.SqlTasks.Collection, Handle)
            .WithName("GetAllSqlTasks")
            .WithTags("Training")
            .Produces<PageResponse<CreateSqlTaskEndpoint.Response>>()
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<Request>>();
    }

    public record Request([FromQuery] int Offset = 0, [FromQuery] int Limit = 20);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Limit).GreaterThan(0).LessThanOrEqualTo(100);
            RuleFor(x => x.Offset).GreaterThanOrEqualTo(0);
        }
    }

    private static async Task<Ok<PageResponse<CreateSqlTaskEndpoint.Response>>> Handle(
        [AsParameters] Request request, TemplateDbContext db, CancellationToken ct)
    {
        var total = await db.SqlTasks.CountAsync(ct);

        var entities = await db.SqlTasks
            .AsNoTracking()
            .OrderBy(x => x.DifficultyLevel)
            .ThenBy(x => x.TaskName)
            .Skip(request.Offset)
            .Take(request.Limit)
            .ToListAsync(ct);

        var items = entities.Select(CreateSqlTaskEndpoint.ToResponse).ToList();

        return TypedResults.Ok(new PageResponse<CreateSqlTaskEndpoint.Response> { Items = items, Count = total });
    }
}
