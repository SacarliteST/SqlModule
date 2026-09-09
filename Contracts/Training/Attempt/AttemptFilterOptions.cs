namespace SQLModule.Contracts.Training.Attempt;

/// <summary>Параметры поиска в справочнике фильтров журнала попыток.</summary>
public record AttemptFilterOptionsRequest(
    string? Search = null,
    string? Id = null,
    int Offset = 0,
    int Limit = 20);

/// <summary>Параметры поиска заданий в справочнике фильтров журнала.</summary>
public record AttemptTaskFilterOptionsRequest(
    string? Search = null,
    string? Id = null,
    string? TopicId = null,
    int Offset = 0,
    int Limit = 20);

/// <summary>Студент, встречающийся в доступном журнале попыток.</summary>
public record AttemptStudentFilterOptionResponse(
    Guid Id,
    string? DisplayName,
    string? Email,
    bool IsAvailable);

/// <summary>Тема, встречающаяся в доступном журнале попыток.</summary>
public record AttemptTopicFilterOptionResponse(
    Guid Id,
    string? Name,
    Guid? ParentId,
    string? Path,
    bool IsAvailable);

/// <summary>Задание, встречающееся в доступном журнале попыток.</summary>
public record AttemptTaskFilterOptionResponse(
    Guid Id,
    string? Name,
    Guid? TopicId,
    string? TopicName,
    bool IsAvailable);
