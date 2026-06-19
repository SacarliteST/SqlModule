using SQLModule.Domain.Training;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Training.Topics;

internal static class TopicErrors
{
    internal static Error NotFound(Guid id) => DomainErrors<Topic>.NotFound(id);
    internal static Error ParentNotFound(Guid id) => DomainErrors<Topic>.Conflict($"Родительская тема '{id}' не найдена.");
    internal static Error Cycle => DomainErrors<Topic>.Conflict("Перемещение создаст цикл в иерархии тем.");
    internal static Error HasSubtopics => DomainErrors<Topic>.Conflict("Тема содержит подтемы и не может быть удалена.");
    internal static Error HasTasks => DomainErrors<Topic>.Conflict("Тема содержит задания и не может быть удалена.");
}
