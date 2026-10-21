using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Persistence;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Attempt;

namespace SQLModule.Application.Training.Attempts.GetAttemptFilterOptions;

/// <summary>Студенты, встречающиеся в журнале попыток.</summary>
public record GetAttemptStudentFilterOptionsQuery(AttemptFilterOptionsRequest Request)
    : IRequest<Result<PageResponse<AttemptStudentFilterOptionResponse>>>;

/// <summary>Темы заданий, встречающиеся в журнале попыток.</summary>
public record GetAttemptTopicFilterOptionsQuery(AttemptFilterOptionsRequest Request)
    : IRequest<Result<PageResponse<AttemptTopicFilterOptionResponse>>>;

/// <summary>Задания, встречающиеся в журнале попыток.</summary>
public record GetAttemptTaskFilterOptionsQuery(AttemptTaskFilterOptionsRequest Request)
    : IRequest<Result<PageResponse<AttemptTaskFilterOptionResponse>>>;

internal static class FilterOptionsParsing
{
    internal static string? NormalizeSearch(string? search)
        => String.IsNullOrWhiteSpace(search) ? null : search.Trim().ToLowerInvariant();

    internal static Guid? ParseGuid(string? value)
        => Guid.TryParse(value, out var parsed) ? parsed : null;

    internal static PageResponse<T> CreatePage<T>(List<T> items, int count) => new()
    {
        Items = items,
        Count = count
    };
}

public sealed class GetAttemptStudentFilterOptionsHandler(IApplicationDbContext db)
    : IRequestHandler<GetAttemptStudentFilterOptionsQuery, Result<PageResponse<AttemptStudentFilterOptionResponse>>>
{
    public async Task<Result<PageResponse<AttemptStudentFilterOptionResponse>>> Handle(
        GetAttemptStudentFilterOptionsQuery command, CancellationToken ct)
    {
        var request = command.Request;
        var search = FilterOptionsParsing.NormalizeSearch(request.Search);
        var id = FilterOptionsParsing.ParseGuid(request.Id);
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

        return FilterOptionsParsing.CreatePage(items, count);
    }
}

public sealed class GetAttemptTopicFilterOptionsHandler(IApplicationDbContext db)
    : IRequestHandler<GetAttemptTopicFilterOptionsQuery, Result<PageResponse<AttemptTopicFilterOptionResponse>>>
{
    public async Task<Result<PageResponse<AttemptTopicFilterOptionResponse>>> Handle(
        GetAttemptTopicFilterOptionsQuery command, CancellationToken ct)
    {
        var request = command.Request;
        var search = FilterOptionsParsing.NormalizeSearch(request.Search);
        var id = FilterOptionsParsing.ParseGuid(request.Id);
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

        return FilterOptionsParsing.CreatePage(items, count);
    }
}

public sealed class GetAttemptTaskFilterOptionsHandler(IApplicationDbContext db)
    : IRequestHandler<GetAttemptTaskFilterOptionsQuery, Result<PageResponse<AttemptTaskFilterOptionResponse>>>
{
    public async Task<Result<PageResponse<AttemptTaskFilterOptionResponse>>> Handle(
        GetAttemptTaskFilterOptionsQuery command, CancellationToken ct)
    {
        var request = command.Request;
        var search = FilterOptionsParsing.NormalizeSearch(request.Search);
        var id = FilterOptionsParsing.ParseGuid(request.Id);
        var topicId = FilterOptionsParsing.ParseGuid(request.TopicId);
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

        return FilterOptionsParsing.CreatePage(items, count);
    }
}
