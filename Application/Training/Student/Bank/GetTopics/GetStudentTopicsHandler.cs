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
        var ownStatistics = await db.SqlTasks.AsNoTracking()
            .Where(task => task.PublicationStatus == PublicationStatus.Published &&
                           task.Visibility == TaskVisibility.Open &&
                           task.BankPublishedAt != null)
            .Select(task => new
            {
                task.TopicId,
                Attempted = db.Attempts.Any(attempt =>
                    attempt.UserId == request.UserId && attempt.TaskId == task.Id &&
                    attempt.ModuleSessionId == null && attempt.FinishedAt >= task.BankPublishedAt),
                Solved = (from attempt in db.Attempts
                          join version in db.TaskValidationVersions on attempt.ValidationVersionId equals version.Id
                          where attempt.UserId == request.UserId && attempt.TaskId == task.Id &&
                                attempt.ModuleSessionId == null && attempt.FinishedAt >= task.BankPublishedAt &&
                                attempt.Score >= version.PassingScore
                          select attempt.Id).Any()
            })
            .GroupBy(task => task.TopicId)
            .Select(group => new
            {
                TopicId = group.Key,
                TaskCount = group.Count(),
                AttemptedCount = group.Count(task => task.Attempted),
                SolvedCount = group.Count(task => task.Solved)
            })
            .ToListAsync(ct);

        var statisticsByTopic = ownStatistics.ToDictionary(
            value => value.TopicId,
            value => (value.TaskCount, value.AttemptedCount, value.SolvedCount));
        var children = topics
            .Where(topic => topic.ParentTopicId.HasValue)
            .GroupBy(topic => topic.ParentTopicId!.Value)
            .ToDictionary(group => group.Key, group => group.Select(topic => topic.Id).ToArray());

        var visible = new List<StudentTopicResponse>();
        foreach (var topic in topics)
        {
            var subtree = DescendantsAndSelf(topic.Id, children);
            var statistics = subtree
                .Select(id => statisticsByTopic.GetValueOrDefault(id))
                .Aggregate(
                    (TaskCount: 0, AttemptedCount: 0, SolvedCount: 0),
                    (total, own) => (
                        total.TaskCount + own.TaskCount,
                        total.AttemptedCount + own.AttemptedCount,
                        total.SolvedCount + own.SolvedCount));
            if (statistics.TaskCount == 0)
            {
                continue;
            }

            visible.Add(new StudentTopicResponse(
                topic.Id,
                topic.TopicName,
                topic.ParentTopicId,
                topic.Description,
                statistics.TaskCount,
                statistics.AttemptedCount,
                statistics.SolvedCount,
                statistics.TaskCount));
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
