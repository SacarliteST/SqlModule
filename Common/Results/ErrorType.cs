namespace SQLModule.Common.Results;

/// <summary>Категория ошибки, используемая для маппинга на HTTP-статус.</summary>
public enum ErrorType
{
    /// <summary>Нарушение бизнес-правила ввода (422).</summary>
    Validation,

    /// <summary>Сущность не найдена (404).</summary>
    NotFound,

    /// <summary>Доступ к найденному ресурсу запрещён (403).</summary>
    Forbidden,

    /// <summary>Конфликт состояния (409).</summary>
    Conflict,

    /// <summary>Условие конкурентного обновления не выполнено (412).</summary>
    PreconditionFailed,

    /// <summary>Внешний движок или инфраструктура временно недоступны (503).</summary>
    Unavailable,

    /// <summary>Слишком много запросов (429).</summary>
    TooManyRequests,

    /// <summary>Внутренняя ошибка (500).</summary>
    Failure
}
