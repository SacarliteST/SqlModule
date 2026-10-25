using SQLModule.Common.Results;
using SQLModule.Domain.Training;

namespace SQLModule.Application.Training.SqlTasks;

public static class SqlTaskErrors
{
    public static Error NotFound(Guid id) => DomainErrors<SqlTask>.NotFound(id);
    public static Error TopicNotFound(Guid id) => DomainErrors<SqlTask>.Conflict($"Тема с id '{id}' не найдена.");
    public static Error HasAttempts =>
        DomainErrors<SqlTask>.Conflict("Задание имеет попытки выполнения и не может быть удалено.");
    public static Error HasModuleSessions =>
        DomainErrors<SqlTask>.Conflict("Задание используется платформенными сессиями и не может быть удалено.");
    public static Error PublishedCannotBeDeleted =>
        DomainErrors<SqlTask>.Conflict("Опубликованное задание сначала нужно архивировать.");
    public static Error InUse =>
        DomainErrors<SqlTask>.Conflict("Задание используется другими данными модуля и не может быть удалено.");
    public static Error PublishRequiresAction =>
        DomainErrors<SqlTask>.Conflict("Переход в Published доступен только через отдельную операцию публикации.");
    public static Error AlreadyPublished(Guid id) =>
        DomainErrors<SqlTask>.Conflict($"Задание '{id}' уже опубликовано.");
    public static Error ArchivedCannotBePublished(Guid id) =>
        DomainErrors<SqlTask>.Conflict($"Архивное задание '{id}' нельзя опубликовать.");
    public static Error ValidationVersionNotPublished =>
        Error.Conflict("SqlTask.ValidationVersionNotPublished", "Оценка решения не опубликована.");
    public static Error ReferenceQueryMissing =>
        Error.Validation("SqlTask.ReferenceQueryMissing", "Эталонный SQL-запрос не задан.");
    public static Error ReferenceQueryNotValidated =>
        DomainErrors<SqlTask>.Validation("Эталонный SQL-запрос необходимо проверить повторно.");
    public static Error HasAttemptsOnPublish =>
        DomainErrors<SqlTask>.Conflict("Задание уже имеет студенческие попытки.");
    public static Error TrainingDatabaseUnavailable =>
        DomainErrors<SqlTask>.Conflict("Учебная база задания недоступна.");
    public static Error LinksChangeRequiresDraft(Guid id) =>
        DomainErrors<SqlTask>.Conflict($"Связи задания '{id}' можно менять только в статусе Draft.");
    public static Error LinksChangeBlockedByAttempts(Guid id) =>
        DomainErrors<SqlTask>.Conflict($"Связи задания '{id}' нельзя менять после появления попыток.");
    public static Error ReferenceChangeRequiresDraft(Guid id) =>
        DomainErrors<SqlTask>.Conflict($"Эталон задания '{id}' можно менять только в статусе Draft.");
    public static Error ReferenceChangeBlockedByAttempts(Guid id) =>
        DomainErrors<SqlTask>.Conflict($"Эталон задания '{id}' нельзя менять после появления попыток.");
    public static Error ReferenceResultExceedsComparisonLimit(int limit) =>
        Error.Validation(
            "ReferenceResultExceedsComparisonLimit",
            $"Результат эталона превышает лимит сравнения {limit} строк. Измените запрос или уменьшите результат.",
            "referenceQuery.queryText",
            limit);
}
