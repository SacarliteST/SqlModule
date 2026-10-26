using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Persistence;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Student;
using SQLModule.Domain.Training;

namespace SQLModule.Application.Training.Student;

/// <summary>Строит дерево тем банка с агрегированной личной статистикой.</summary>
public sealed class GetStudentTopicsHandler
    : IRequestHandler<GetStudentTopicsQuery, Result<PageResponse<StudentTopicResponse>>>
{
    private readonly IApplicationDbContext db;

    /// <summary>Создаёт обработчик каталога тем.</summary>
    public GetStudentTopicsHandler(IApplicationDbContext db)
    {
        this.db = db;
    }

    /// <inheritdoc />
    public async Task<Result<PageResponse<StudentTopicResponse>>> Handle(
        GetStudentTopicsQuery request,
        CancellationToken ct)
    {
        var topics = await db.Topics.AsNoTracking()
            .Select(topic => new { topic.Id, topic.TopicName, topic.ParentTopicId, topic.Description })
            .ToListAsync(ct);
        var tasks = await db.SqlTasks.AsNoTracking()
            .Where(task => task.PublicationStatus == PublicationStatus.Published &&
                           task.Visibility == TaskVisibility.Open &&
                           task.BankPublishedAt != null)
            .Select(task => new { task.Id, task.TopicId, PublishedAt = task.BankPublishedAt!.Value })
            .ToListAsync(ct);
        var versions = db.TaskValidationVersions.AsNoTracking();
        var attempts = await (
            from attempt in db.Attempts.AsNoTracking()
            join version in versions on attempt.ValidationVersionId equals version.Id
            where attempt.UserId == request.UserId && attempt.ModuleSessionId == null
            select new { attempt.TaskId, attempt.Score, attempt.FinishedAt, version.PassingScore })
            .ToListAsync(ct);

        var tasksById = tasks.ToDictionary(task => task.Id);
        var validAttempts = attempts
            .Where(attempt => tasksById.TryGetValue(attempt.TaskId, out var task) &&
                              attempt.FinishedAt >= task.PublishedAt)
            .GroupBy(attempt => attempt.TaskId)
            .ToDictionary(
                group => group.Key,
                group => (Attempted: true, Solved: group.Any(attempt => attempt.Score >= attempt.PassingScore)));
        var children = topics
            .Where(topic => topic.ParentTopicId.HasValue)
            .GroupBy(topic => topic.ParentTopicId!.Value)
            .ToDictionary(group => group.Key, group => group.Select(topic => topic.Id).ToArray());
        var ownTasks = tasks.GroupBy(task => task.TopicId)
            .ToDictionary(group => group.Key, group => group.Select(task => task.Id).ToArray());

        var visible = new List<StudentTopicResponse>();
        foreach (var topic in topics)
        {
            var subtree = DescendantsAndSelf(topic.Id, children);
            var taskIds = subtree.SelectMany(id => ownTasks.GetValueOrDefault(id) ?? []).Distinct().ToArray();
            if (taskIds.Length == 0)
            {
                continue;
            }

            visible.Add(new StudentTopicResponse(
                topic.Id,
                topic.TopicName,
                topic.ParentTopicId,
                topic.Description,
                taskIds.Length,
                taskIds.Count(validAttempts.ContainsKey),
                taskIds.Count(id => validAttempts.GetValueOrDefault(id).Solved)));
        }

        var ordered = visible.OrderBy(topic => topic.TopicName).ThenBy(topic => topic.Id).ToArray();
        return new PageResponse<StudentTopicResponse>
        {
            Items = ordered.Skip(request.Offset).Take(request.Limit).ToList(),
            Count = ordered.Length
        };
    }

    private static IReadOnlySet<Guid> DescendantsAndSelf(
        Guid topicId,
        IReadOnlyDictionary<Guid, Guid[]> children)
    {
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
}
