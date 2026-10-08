using SQLModule.Common.Results;

namespace SQLModule.Application.Training.Progress;

public static class ProgressErrors
{
    public static Error TaskNotFound(Guid taskId) => Error.NotFound("SqlTask", taskId);

    public static Error ValidationVersionNotPublished => Error.Conflict(
        "Progress.ValidationVersionNotPublished",
        "Для задания ещё не опубликована конфигурация проверки.");


    public static Error NotFound => Error.Conflict(
        "Progress.NotFound",
        "Активное standalone-прохождение не найдено.");

    public static Error StillActive => Error.Conflict(
        "Progress.StillActive",
        "Текущее прохождение ещё можно продолжить.");

    public static Error AttemptsExhausted => Error.Conflict(
        "Progress.AttemptsExhausted",
        "Лимит попыток исчерпан.");

    public static Error Closed => Error.Conflict(
        "Progress.Closed",
        "Прохождение уже завершено.");

    public static Error IdempotencyPayloadMismatch => Error.Conflict(
        "IdempotencyKeyPayloadMismatch",
        "Idempotency-Key уже использован с другим запросом.");
}
