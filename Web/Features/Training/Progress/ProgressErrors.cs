using SQLModule.Common.Results;

namespace SQLModule.Web.Features.Training.Progress;

internal static class ProgressErrors
{
    internal static Error TaskNotFound(Guid taskId) => Error.NotFound("SqlTask", taskId);

    internal static Error ValidationVersionNotPublished => Error.Conflict(
        "Progress.ValidationVersionNotPublished",
        "Для задания ещё не опубликована конфигурация проверки.");


    internal static Error NotFound => Error.Conflict(
        "Progress.NotFound",
        "Активное standalone-прохождение не найдено.");

    internal static Error StillActive => Error.Conflict(
        "Progress.StillActive",
        "Текущее прохождение ещё можно продолжить.");

    internal static Error AttemptsExhausted => Error.Conflict(
        "Progress.AttemptsExhausted",
        "Лимит попыток исчерпан.");

    internal static Error Closed => Error.Conflict(
        "Progress.Closed",
        "Прохождение уже завершено.");

    internal static Error IdempotencyPayloadMismatch => Error.Conflict(
        "IdempotencyKeyPayloadMismatch",
        "Idempotency-Key уже использован с другим запросом.");
}
