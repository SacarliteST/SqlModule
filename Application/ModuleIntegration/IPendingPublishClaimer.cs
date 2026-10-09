namespace SQLModule.Application.ModuleIntegration;

/// <summary>
/// Атомарно закрепляет за экземпляром пачку сообщений outbox, чтобы несколько экземпляров приложения
/// не отправляли одно и то же сообщение дважды. Закрепление — аренда на ограниченное время: если экземпляр
/// упал, сообщение вернётся в обработку после истечения аренды.
/// </summary>
public interface IPendingPublishClaimer
{
    /// <summary>
    /// Забирает до <paramref name="batchSize"/> готовых к отправке сообщений (не отправленных, не в dead-letter,
    /// с наступившим <c>NextAttemptAt</c> и без действующей чужой аренды) и возвращает их идентификаторы.
    /// </summary>
    Task<IReadOnlyList<Guid>> ClaimAsync(
        string owner,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        int batchSize,
        CancellationToken ct);
}
