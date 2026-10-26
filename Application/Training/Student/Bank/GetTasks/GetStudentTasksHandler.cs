using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Persistence;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Student;
using SQLModule.Domain.Training;

namespace SQLModule.Application.Training.Student;

/// <summary>Возвращает отфильтрованный каталог банка с личной статистикой.</summary>
public sealed class GetStudentTasksHandler
    : IRequestHandler<GetStudentTasksQuery, Result<PageResponse<StudentTaskResponse>>>
{
    private readonly IApplicationDbContext db;

    /// <summary>Создаёт обработчик каталога заданий.</summary>
    public GetStudentTasksHandler(IApplicationDbContext db)
    {
        this.db = db;
    }

    /// <inheritdoc />
    public async Task<Result<PageResponse<StudentTaskResponse>>> Handle(
        GetStudentTasksQuery command,
        CancellationToken ct)
    {
        var request = command.Request;
        var allowedTopics = request.TopicId.HasValue
            ? await GetDescendantsAndSelfAsync(request.TopicId.Value, ct)
            : null;
        var query = db.SqlTasks.AsNoTracking().Where(task =>
            task.PublicationStatus == PublicationStatus.Published &&
            task.Visibility == TaskVisibility.Open &&
            task.BankNumber != null &&
            task.BankPublishedAt != null);
        if (allowedTopics is not null)
        {
            query = query.Where(task => allowedTopics.Contains(task.TopicId));
        }

        if (request.DifficultyLevel.HasValue)
        {
            query = query.Where(task => task.DifficultyLevel == request.DifficultyLevel.Value);
        }

        if (request.BankNumber.HasValue)
        {
            query = query.Where(task => task.BankNumber == request.BankNumber.Value);
        }

        if (!String.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(task =>
                task.TaskName.ToLower().Contains(search) || task.TaskText.ToLower().Contains(search));
        }

        var tasks = await query.Select(task => new
        {
            task.Id,
            BankNumber = task.BankNumber!.Value,
            task.TopicId,
            task.Topic.TopicName,
            task.TaskName,
            task.TaskText,
            task.DifficultyLevel,
            DbmsName = db.TargetDbs.Where(target => target.Id == task.SqlQuery.TargetDbId)
                .Select(target => target.Dbms.DbmsName).First(),
            BankPublishedAt = task.BankPublishedAt!.Value
        }).ToListAsync(ct);
        var taskById = tasks.ToDictionary(task => task.Id);
        var taskIds = taskById.Keys.ToArray();
        var attempts = await (
            from attempt in db.Attempts.AsNoTracking()
            join version in db.TaskValidationVersions.AsNoTracking()
                on attempt.ValidationVersionId equals version.Id
            where attempt.UserId == command.UserId &&
                  attempt.ModuleSessionId == null &&
                  taskIds.Contains(attempt.TaskId)
            select new { attempt.TaskId, attempt.Score, attempt.FinishedAt, version.PassingScore })
            .ToListAsync(ct);
        var statistics = attempts
            .Where(attempt => attempt.FinishedAt >= taskById[attempt.TaskId].BankPublishedAt)
            .GroupBy(attempt => attempt.TaskId)
            .ToDictionary(
                group => group.Key,
                group => new
                {
                    Count = group.Count(),
                    BestScore = group.Max(attempt => attempt.Score),
                    LastAttemptAt = group.Max(attempt => attempt.FinishedAt),
                    Solved = group.Any(attempt => attempt.Score >= attempt.PassingScore)
                });

        var items = tasks.Select(task =>
        {
            var hasStatistics = statistics.TryGetValue(task.Id, out var statistic);
            var status = !hasStatistics
                ? BankTaskStatus.NotStarted
                : statistic!.Solved ? BankTaskStatus.Solved : BankTaskStatus.InProgress;
            return new StudentTaskResponse(
                task.Id,
                task.BankNumber,
                task.TopicId,
                task.TopicName,
                task.TaskName,
                task.TaskText.Length <= 200 ? task.TaskText : task.TaskText[..200],
                task.DifficultyLevel,
                task.DbmsName,
                status,
                statistic?.Count ?? 0,
                statistic?.BestScore,
                statistic?.LastAttemptAt,
                task.BankPublishedAt);
        });
        if (request.Solved.HasValue)
        {
            items = items.Where(item => (item.Status == BankTaskStatus.Solved) == request.Solved.Value);
        }

        items = ApplySorting(items, request.SortBy, request.Descending);
        var materialized = items.ToArray();
        return new PageResponse<StudentTaskResponse>
        {
            Items = materialized.Skip(request.Offset).Take(request.Limit).ToList(),
            Count = materialized.Length
        };
    }

    private async Task<HashSet<Guid>> GetDescendantsAndSelfAsync(Guid topicId, CancellationToken ct)
    {
        var relations = await db.Topics.AsNoTracking()
            .Select(topic => new { topic.Id, topic.ParentTopicId })
            .ToListAsync(ct);
        var children = relations.Where(topic => topic.ParentTopicId.HasValue)
            .GroupBy(topic => topic.ParentTopicId!.Value)
            .ToDictionary(group => group.Key, group => group.Select(topic => topic.Id).ToArray());
        var result = new HashSet<Guid>();
        var pending = new Stack<Guid>();
        pending.Push(topicId);
        while (pending.TryPop(out var current))
        {
            if (!result.Add(current))
            {
                continue;
            }

            foreach (var child in children.GetValueOrDefault(current) ?? [])
            {
                pending.Push(child);
            }
        }

        return result;
    }

    private static IEnumerable<StudentTaskResponse> ApplySorting(
        IEnumerable<StudentTaskResponse> source,
        BankTaskSort sort,
        bool descending) => (sort, descending) switch
    {
        (BankTaskSort.Difficulty, false) => source.OrderBy(item => item.DifficultyLevel).ThenBy(item => item.BankNumber),
        (BankTaskSort.Difficulty, true) => source.OrderByDescending(item => item.DifficultyLevel).ThenBy(item => item.BankNumber),
        (BankTaskSort.PublishedAt, false) => source.OrderBy(item => item.BankPublishedAt).ThenBy(item => item.BankNumber),
        (BankTaskSort.PublishedAt, true) => source.OrderByDescending(item => item.BankPublishedAt).ThenBy(item => item.BankNumber),
        (_, true) => source.OrderByDescending(item => item.BankNumber),
        _ => source.OrderBy(item => item.BankNumber)
    };
}
