namespace SQLModule.Application.Training.Bank;

/// <summary>Ограничивает параллельность и частоту тренировочных отправок студента.</summary>
public interface ITrainingAttemptGate
{
    /// <summary>Рекомендуемая задержка до повторной отправки.</summary>
    int RetryAfterSeconds { get; }

    /// <summary>Пытается получить разрешение на тренировочную попытку.</summary>
    /// <param name="userId">Идентификатор студента.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Lease или null, если ограничение превышено.</returns>
    ValueTask<TrainingAttemptLease?> TryAcquireAsync(Guid userId, CancellationToken ct);
}
