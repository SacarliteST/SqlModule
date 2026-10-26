using SQLModule.Contracts.Training.SqlTask;

namespace SQLModule.Application.Training.Bank;

/// <summary>Проверяет готовность задания к публикации в открытом банке.</summary>
public interface ITaskBankReadiness
{
    /// <summary>Возвращает полное состояние готовности задания.</summary>
    /// <param name="taskId">Идентификатор задания.</param>
    /// <param name="ct">Токен отмены.</param>
    Task<TaskBankReadinessResponse> EvaluateAsync(Guid taskId, CancellationToken ct);

    /// <summary>Проверяет наличие содержательной автоматической подсказки.</summary>
    /// <param name="taskId">Идентификатор задания.</param>
    /// <param name="ct">Токен отмены.</param>
    Task<bool> HasAutomaticHintAsync(Guid taskId, CancellationToken ct);
}
