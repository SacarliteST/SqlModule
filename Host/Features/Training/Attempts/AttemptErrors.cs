using SQLModule.Domain.Training;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Training.Attempts;

internal static class AttemptErrors
{
    internal static Error NotFound(Guid id) => DomainErrors<Attempt>.NotFound(id);
    internal static Error TaskNotFound(Guid id) => DomainErrors<Attempt>.Conflict($"Задание '{id}' не найдено.");
    internal static Error QueryNotFound(Guid id) => DomainErrors<Attempt>.Conflict($"Эталонный запрос '{id}' не найден.");
    internal static Error InvalidTimeRange => DomainErrors<Attempt>.Validation("Время завершения раньше начала.");
}
