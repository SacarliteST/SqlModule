using SQLModule.Contracts.Training.Episode;

namespace SQLModule.Client.Episode;

/// <summary>Клиент student-safe жизненного цикла учебного эпизода.</summary>
public interface ILearningEpisodeClient
{
    /// <summary>Начинает новый учебный эпизод или возвращает уже активный эпизод задания.</summary>
    Task<LearningEpisodeResponse> StartAsync(Guid taskId, CancellationToken ct = default);

    /// <summary>Продлевает активность учебного эпизода.</summary>
    Task<LearningEpisodeResponse> HeartbeatAsync(Guid episodeId, CancellationToken ct = default);

    /// <summary>Финализирует учебный эпизод выбранным студентом способом.</summary>
    Task<LearningEpisodeResponse> FinalizeAsync(
        Guid episodeId,
        LearningEpisodeFinalizationMode mode,
        Guid? idempotencyKey = null,
        CancellationToken ct = default);
}
