using SQLModule.Common.Results;
using SQLModule.Domain.Training;

namespace SQLModule.Host.Features.Training.SqlTasks;

internal static class SqlTaskErrors
{
    internal static Error NotFound(Guid id) => DomainErrors<SqlTask>.NotFound(id);
    internal static Error TopicNotFound(Guid id) => DomainErrors<SqlTask>.Conflict($"Тема с id '{id}' не найдена.");
    internal static Error QueryNotFound(Guid id) => DomainErrors<SqlTask>.Conflict($"SQL-запрос с id '{id}' не найден.");
    internal static Error HasAttempts(Guid id) => DomainErrors<SqlTask>.Conflict($"Задание '{id}' имеет попытки выполнения и не может быть удалено.");
}
