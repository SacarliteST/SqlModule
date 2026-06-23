using SQLModule.Common.Results;
using SQLModule.Domain.Training;

namespace SQLModule.Host.Features.Training.Attempts;

internal static class AttemptErrors
{
    internal static Error NotFound(Guid id) => DomainErrors<Attempt>.NotFound(id);
    internal static Error TaskNotFound(Guid id) => DomainErrors<Attempt>.NotFound($"Задание '{id}' не найдено.");
    internal static Error ReferenceNotReady => DomainErrors<Attempt>.Conflict("Эталонный результат задания ещё не вычислен.");
}
