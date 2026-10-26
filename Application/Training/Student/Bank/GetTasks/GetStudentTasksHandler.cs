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

        var tasks = query.Select(task => new CatalogItem
        {
            Id = task.Id,
            BankNumber = task.BankNumber!.Value,
            TopicId = task.TopicId,
            TopicName = task.Topic.TopicName,
            TaskName = task.TaskName,
            TaskText = task.TaskText,
            DifficultyLevel = task.DifficultyLevel,
            DbmsName = db.TargetDbs.Where(target => target.Id == task.SqlQuery.TargetDbId)
                .Select(target => target.Dbms.DbmsName).First(),
            BankPublishedAt = task.BankPublishedAt!.Value,
            AttemptsCount = db.Attempts.Count(attempt =>
                attempt.UserId == command.UserId && attempt.TaskId == task.Id &&
                attempt.ModuleSessionId == null && attempt.FinishedAt >= task.BankPublishedAt),
            BestScore = db.Attempts.Where(attempt =>
                    attempt.UserId == command.UserId && attempt.TaskId == task.Id &&
                    attempt.ModuleSessionId == null && attempt.FinishedAt >= task.BankPublishedAt)
                .Max(attempt => (int?)attempt.Score),
            LastAttemptAt = db.Attempts.Where(attempt =>
                    attempt.UserId == command.UserId && attempt.TaskId == task.Id &&
                    attempt.ModuleSessionId == null && attempt.FinishedAt >= task.BankPublishedAt)
                .Max(attempt => (DateTimeOffset?)attempt.FinishedAt),
            Solved = (from attempt in db.Attempts
                      join version in db.TaskValidationVersions on attempt.ValidationVersionId equals version.Id
                      where attempt.UserId == command.UserId && attempt.TaskId == task.Id &&
                            attempt.ModuleSessionId == null && attempt.FinishedAt >= task.BankPublishedAt &&
                            attempt.Score >= version.PassingScore
                      select attempt.Id).Any()
        });
        if (request.Solved.HasValue)
        {
            tasks = tasks.Where(item => item.Solved == request.Solved.Value);
        }

        tasks = ApplySorting(tasks, request.SortBy, request.Descending);
        var count = await tasks.CountAsync(ct);
        var page = await tasks.Skip(request.Offset).Take(request.Limit).ToArrayAsync(ct);
        var items = page.Select(task => new StudentTaskResponse(
                task.Id,
                task.BankNumber,
                task.TopicId,
                task.TopicName,
                task.TaskName,
                task.TaskText.Length <= 200 ? task.TaskText : task.TaskText[..200],
                task.DifficultyLevel,
                task.DbmsName,
                task.AttemptsCount == 0 ? BankTaskStatus.NotStarted : task.Solved ? BankTaskStatus.Solved : BankTaskStatus.InProgress,
                task.AttemptsCount,
                task.BestScore,
                task.LastAttemptAt,
                task.BankPublishedAt)).ToList();
        return new PageResponse<StudentTaskResponse>
        {
            Items = items,
            Count = count
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

    private static IQueryable<CatalogItem> ApplySorting(
        IQueryable<CatalogItem> source,
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
