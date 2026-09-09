using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Attempt;
using SQLModule.Data.Core;
using SQLModule.Web.Common;

namespace SQLModule.Web.Features.Training.Attempts;

/// <summary>Справочники значений, реально встречающихся в журнале попыток.</summary>
public sealed class GetAttemptFilterOptionsEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        MapStudents(app);
        MapTopics(app);
        MapTasks(app);
    }

    private static void MapStudents(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Training.Attempts.StudentFilterOptions, GetStudents)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("GetAttemptStudentFilterOptions")
            .WithTags("Training")
            .WithSummary("Студенты для фильтра журнала попыток")
            .WithDescription("Возвращает уникальных студентов, встречающихся в доступном журнале. Поддерживает поиск по имени/email, восстановление по id и серверную пагинацию.")
            .Produces<PageResponse<AttemptStudentFilterOptionResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .AddEndpointFilter<ValidationFilter<AttemptFilterOptionsRequest>>();
    }

    private static void MapTopics(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Training.Attempts.TopicFilterOptions, GetTopics)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("GetAttemptTopicFilterOptions")
            .WithTags("Training")
            .WithSummary("Темы для фильтра журнала попыток")
            .WithDescription("Возвращает уникальные темы заданий, встречающихся в доступном журнале, включая архивные данные с попытками.")
            .Produces<PageResponse<AttemptTopicFilterOptionResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .AddEndpointFilter<ValidationFilter<AttemptFilterOptionsRequest>>();
    }

    private static void MapTasks(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Training.Attempts.TaskFilterOptions, GetTasks)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("GetAttemptTaskFilterOptions")
            .WithTags("Training")
            .WithSummary("Задания для фильтра журнала попыток")
            .WithDescription("Возвращает уникальные задания, встречающиеся в доступном журнале. Поддерживает поиск, фильтр по теме, восстановление по id и пагинацию.")
            .Produces<PageResponse<AttemptTaskFilterOptionResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .AddEndpointFilter<ValidationFilter<AttemptTaskFilterOptionsRequest>>();
    }

    private static async Task<IResult> GetStudents(
        [AsParameters] AttemptFilterOptionsRequest request,
        AppDbContext db,
        CancellationToken ct)
    {
        var search = NormalizeSearch(request.Search);
        var id = ParseGuid(request.Id);
        var attempts = db.Attempts.AsNoTracking()
            .Where(x => !id.HasValue || x.UserId == id.Value);
        if (search is not null)
        {
            attempts = attempts.Where(x =>
                x.StudentName.ToLower().Contains(search) ||
                (x.StudentEmail != null && x.StudentEmail.ToLower().Contains(search)));
        }

        var query = attempts
            .GroupBy(x => x.UserId)
            .Select(group => new
            {
                Id = group.Key,
                DisplayName = group.Max(x => x.StudentName),
                Email = group.Max(x => x.StudentEmail)
            });

        var count = await query.CountAsync(ct);
        var rows = await query
            .OrderBy(x => x.DisplayName)
            .ThenBy(x => x.Email)
            .ThenBy(x => x.Id)
            .Skip(request.Offset)
            .Take(request.Limit)
            .ToListAsync(ct);
        var items = rows.Select(x => new AttemptStudentFilterOptionResponse(
            x.Id, x.DisplayName, x.Email, true)).ToList();

        return Results.Ok(CreatePage(items, count));
    }

    private static async Task<IResult> GetTopics(
        [AsParameters] AttemptFilterOptionsRequest request,
        AppDbContext db,
        CancellationToken ct)
    {
        var search = NormalizeSearch(request.Search);
        var id = ParseGuid(request.Id);
        var tasks = db.Attempts.AsNoTracking()
            .Join(db.SqlTasks.AsNoTracking(), attempt => attempt.TaskId, task => task.Id, (_, task) => task)
            .Where(task => !id.HasValue || task.TopicId == id.Value);
        if (search is not null)
        {
            tasks = tasks.Where(task => task.Topic.TopicName.ToLower().Contains(search));
        }

        var query = tasks
            .Select(task => new
            {
                Id = task.Topic.Id,
                Name = task.Topic.TopicName,
                ParentId = task.Topic.ParentTopicId
            })
            .Distinct();

        var count = await query.CountAsync(ct);
        var rows = await query
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Id)
            .Skip(request.Offset)
            .Take(request.Limit)
            .ToListAsync(ct);
        var items = rows.Select(x => new AttemptTopicFilterOptionResponse(
            x.Id, x.Name, x.ParentId, x.Name, true)).ToList();

        return Results.Ok(CreatePage(items, count));
    }

    private static async Task<IResult> GetTasks(
        [AsParameters] AttemptTaskFilterOptionsRequest request,
        AppDbContext db,
        CancellationToken ct)
    {
        var search = NormalizeSearch(request.Search);
        var id = ParseGuid(request.Id);
        var topicId = ParseGuid(request.TopicId);
        var tasks = db.Attempts.AsNoTracking()
            .Join(db.SqlTasks.AsNoTracking(), attempt => attempt.TaskId, task => task.Id, (_, task) => task)
            .Where(task => !id.HasValue || task.Id == id.Value)
            .Where(task => !topicId.HasValue || task.TopicId == topicId.Value);
        if (search is not null)
        {
            tasks = tasks.Where(task => task.TaskName.ToLower().Contains(search));
        }

        var query = tasks
            .Select(task => new
            {
                task.Id,
                Name = task.TaskName,
                task.TopicId,
                TopicName = task.Topic.TopicName
            })
            .Distinct();

        var count = await query.CountAsync(ct);
        var rows = await query
            .OrderBy(x => x.Name)
            .ThenBy(x => x.TopicName)
            .ThenBy(x => x.Id)
            .Skip(request.Offset)
            .Take(request.Limit)
            .ToListAsync(ct);
        var items = rows.Select(x => new AttemptTaskFilterOptionResponse(
            x.Id, x.Name, x.TopicId, x.TopicName, true)).ToList();

        return Results.Ok(CreatePage(items, count));
    }

    private static string? NormalizeSearch(string? search)
        => String.IsNullOrWhiteSpace(search) ? null : search.Trim().ToLowerInvariant();

    private static Guid? ParseGuid(string? value)
        => Guid.TryParse(value, out var parsed) ? parsed : null;

    private static PageResponse<T> CreatePage<T>(List<T> items, int count) => new()
    {
        Items = items,
        Count = count
    };
}
