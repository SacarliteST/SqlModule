using SQLModule.Common.Results;
using SQLModule.Domain.Training;

namespace SQLModule.Web.Features.Training.Attempts;

internal static class AttemptErrors
{
    internal static Error NotFound(Guid id) => DomainErrors<Attempt>.NotFound(id);
    internal static Error TaskNotFound(Guid id) => DomainErrors<Attempt>.NotFound($"Задание '{id}' не найдено.");
    internal static Error ReferenceNotReady => DomainErrors<Attempt>.Conflict("Эталонный результат задания ещё не вычислен.");
    internal static Error ModuleSessionRequired => Error.Conflict(
        "ModuleSessionRequired", "Для отправки решения требуется активная платформенная сессия.");
    internal static Error ModuleSessionForbidden => Error.Forbidden(
        "ModuleSessionForbidden", "Платформенная сессия принадлежит другому пользователю.");
    internal static Error SessionTaskMismatch => Error.Conflict(
        "SessionTaskMismatch", "Задание не соответствует текущей платформенной сессии.");
    internal static Error ModuleSessionClosed => Error.Conflict(
        "ModuleSessionClosed", "Платформенная сессия завершена или срок её действия истёк.");
}
