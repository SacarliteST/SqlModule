using SQLModule.Common.Results;
using SQLModule.Domain.Training;

namespace SQLModule.Application.Training.Attempts;

public static class AttemptErrors
{
    public static Error NotFound(Guid id) => DomainErrors<Attempt>.NotFound(id);
    public static Error TaskNotFound(Guid id) => DomainErrors<Attempt>.NotFound($"Задание '{id}' не найдено.");
    public static Error ReferenceNotReady => DomainErrors<Attempt>.Conflict("Эталонный результат задания ещё не вычислен.");
}
