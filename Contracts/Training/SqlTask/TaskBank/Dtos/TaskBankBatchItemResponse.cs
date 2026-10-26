namespace SQLModule.Contracts.Training.SqlTask;

/// <summary>Результат пакетной операции для одного задания.</summary>
/// <param name="TaskId">Идентификатор задания.</param>
/// <param name="Updated">Изменено ли состояние задания.</param>
/// <param name="State">Новое состояние при успехе.</param>
/// <param name="Blockers">Причины пропуска.</param>
public sealed record TaskBankBatchItemResponse(
    Guid TaskId,
    bool Updated,
    TaskBankStateResponse? State,
    IReadOnlyList<TaskBankBlockerResponse> Blockers);
