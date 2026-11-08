using System.Text.Json.Serialization;

namespace SQLModule.Contracts.Training.Episode;

[JsonConverter(typeof(JsonStringEnumConverter<LearningEpisodeStatusResponse>))]
public enum LearningEpisodeStatusResponse
{
    Active,
    Finalized
}

[JsonConverter(typeof(JsonStringEnumConverter<LearningEpisodeFinalizationMode>))]
public enum LearningEpisodeFinalizationMode
{
    AcceptPartialResult,
    Abandon
}

/// <summary>Явное решение студента окончательно закрыть текущий учебный эпизод.</summary>
public sealed record FinalizeLearningEpisodeRequest(LearningEpisodeFinalizationMode? Mode);

/// <summary>Безопасное состояние текущего учебного эпизода.</summary>
public sealed record LearningEpisodeResponse(
    Guid EpisodeId,
    Guid TaskId,
    Guid ValidationVersionId,
    LearningEpisodeStatusResponse Status,
    DateTimeOffset StartedAt,
    DateTimeOffset LastHeartbeatAt,
    DateTimeOffset? FinalizedAt,
    Guid? FinalAttemptId,
    bool ProducesObservation);
