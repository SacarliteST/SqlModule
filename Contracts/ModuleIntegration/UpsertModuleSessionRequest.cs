namespace SQLModule.Contracts.ModuleIntegration;

/// <summary>Доверенный контекст платформенной сессии, отправляемый Education.</summary>
/// <param name="SessionId">Идентификатор сессии Education.</param>
/// <param name="SessionKey">Секрет сессии для Kafka-событий и отправки оценки.</param>
/// <param name="UserId">Идентификатор студента.</param>
/// <param name="TaskRef">Непрозрачная ссылка на задание; для SQL-модуля это SqlTask.Id.</param>
/// <param name="ReturnUrl">Канонический адрес возврата в платформу.</param>
/// <param name="ExpiresAt">Необязательное время окончания сессии.</param>
public sealed record UpsertModuleSessionRequest(
    Guid? SessionId,
    string? SessionKey,
    Guid? UserId,
    string? TaskRef,
    string? ReturnUrl,
    DateTimeOffset? ExpiresAt);
