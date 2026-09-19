using SQLModule.Common.Results;
using SQLModule.Domain.Training;

namespace SQLModule.Web.Features.Training.SqlTasks;

internal static class SqlTaskErrors
{
    internal static Error NotFound(Guid id) => DomainErrors<SqlTask>.NotFound(id);
    internal static Error TopicNotFound(Guid id) => DomainErrors<SqlTask>.Conflict($"Тема с id '{id}' не найдена.");
    internal static Error HasAttempts(Guid id) => DomainErrors<SqlTask>.Conflict($"Задание '{id}' имеет попытки выполнения и не может быть удалено.");
    internal static Error PublishRequiresAction =>
        DomainErrors<SqlTask>.Conflict("Переход в Published доступен только через отдельную операцию публикации.");
    internal static Error AlreadyPublished(Guid id) =>
        DomainErrors<SqlTask>.Conflict($"Задание '{id}' уже опубликовано.");
    internal static Error ArchivedCannotBePublished(Guid id) =>
        DomainErrors<SqlTask>.Conflict($"Архивное задание '{id}' нельзя опубликовать.");
    internal static Error ValidationVersionNotPublished =>
        Error.Conflict("SqlTask.ValidationVersionNotPublished", "Оценка решения не опубликована.");
    internal static Error ReferenceQueryMissing =>
        Error.Validation("SqlTask.ReferenceQueryMissing", "Эталонный SQL-запрос не задан.");
    internal static Error ReferenceQueryNotValidated =>
        DomainErrors<SqlTask>.Validation("Эталонный SQL-запрос необходимо проверить повторно.");
    internal static Error HasAttemptsOnPublish =>
        DomainErrors<SqlTask>.Conflict("Задание уже имеет студенческие попытки.");
    internal static Error TrainingDatabaseUnavailable =>
        DomainErrors<SqlTask>.Conflict("Учебная база задания недоступна.");
    internal static Error LinksChangeRequiresDraft(Guid id) =>
        DomainErrors<SqlTask>.Conflict($"Связи задания '{id}' можно менять только в статусе Draft.");
    internal static Error LinksChangeBlockedByAttempts(Guid id) =>
        DomainErrors<SqlTask>.Conflict($"Связи задания '{id}' нельзя менять после появления попыток.");
    internal static Error ReferenceChangeRequiresDraft(Guid id) =>
        DomainErrors<SqlTask>.Conflict($"Эталон задания '{id}' можно менять только в статусе Draft.");
    internal static Error ReferenceChangeBlockedByAttempts(Guid id) =>
        DomainErrors<SqlTask>.Conflict($"Эталон задания '{id}' нельзя менять после появления попыток.");
    internal static Error ReferenceResultExceedsComparisonLimit(int limit) =>
        Error.Validation(
            "ReferenceResultExceedsComparisonLimit",
            $"Результат эталона превышает лимит сравнения {limit} строк. Измените запрос или уменьшите результат.",
            "referenceQuery.queryText",
            limit);
}
