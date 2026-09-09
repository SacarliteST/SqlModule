namespace SQLModule.Contracts.ModuleIntegration;

/// <summary>Канонический контекст текущего платформенного запуска.</summary>
/// <param name="TaskId">Идентификатор SQL-задания, доверенно полученный от Education.</param>
/// <param name="ReturnUrl">Канонический адрес возврата в платформу.</param>
/// <param name="ExpiresAt">Время окончания сессии или null для сессии без лимита.</param>
public sealed record CurrentModuleSessionResponse(
    Guid TaskId,
    string ReturnUrl,
    DateTimeOffset? ExpiresAt);
