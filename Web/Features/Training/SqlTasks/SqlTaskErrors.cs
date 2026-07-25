using SQLModule.Common.Results;
using SQLModule.Domain.Training;

namespace SQLModule.Web.Features.Training.SqlTasks;

internal static class SqlTaskErrors
{
    internal static Error NotFound(Guid id) => DomainErrors<SqlTask>.NotFound(id);
    internal static Error TopicNotFound(Guid id) => DomainErrors<SqlTask>.Conflict($"Тема с id '{id}' не найдена.");
    internal static Error QueryNotFound(Guid id) => DomainErrors<SqlTask>.Conflict($"SQL-запрос с id '{id}' не найден.");
    internal static Error HasAttempts(Guid id) => DomainErrors<SqlTask>.Conflict($"Задание '{id}' имеет попытки выполнения и не может быть удалено.");
    internal static Error InitialStatusMustBeDraft =>
        DomainErrors<SqlTask>.Validation("Новое задание можно создать только в статусе Draft. Используйте отдельную операцию публикации.");
    internal static Error PublishRequiresAction =>
        DomainErrors<SqlTask>.Conflict("Переход в Published доступен только через отдельную операцию публикации.");
    internal static Error AlreadyPublished(Guid id) =>
        DomainErrors<SqlTask>.Conflict($"Задание '{id}' уже опубликовано.");
    internal static Error ArchivedCannotBePublished(Guid id) =>
        DomainErrors<SqlTask>.Conflict($"Архивное задание '{id}' нельзя опубликовать.");
    internal static Error ReferenceQueryNotValidated(Guid id) =>
        DomainErrors<SqlTask>.Validation($"Эталонный SQL-запрос задания '{id}' не прошёл проверку.");
    internal static Error HasAttemptsOnPublish(Guid id) =>
        DomainErrors<SqlTask>.Conflict($"Задание '{id}' имеет попытки выполнения и не может быть опубликовано.");
    internal static Error TrainingDatabaseUnavailable(Guid id) =>
        DomainErrors<SqlTask>.Conflict($"Учебная база данных задания '{id}' недоступна.");
}
