using SQLModule.Common.Results;
using SQLModule.Domain.Training;

namespace SQLModule.Application.Training.Topics;

public static class TopicErrors
{
    public static Error NotFound(Guid id) => DomainErrors<Topic>.NotFound(id);
    public static Error ParentNotFound(Guid id) => DomainErrors<Topic>.Conflict($"Родительская тема '{id}' не найдена.");
    public static Error Cycle => DomainErrors<Topic>.Conflict("Перемещение создаст цикл в иерархии тем.");
    public static Error HasSubtopics => DomainErrors<Topic>.Conflict("Тема содержит подтемы и не может быть удалена.");
    public static Error HasTasks => DomainErrors<Topic>.Conflict("Тема содержит задания и не может быть удалена.");
}
