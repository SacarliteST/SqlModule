namespace SQLModule.Contracts.Training.SqlTask;

/// <summary>Готовность задания к открытию в банке.</summary>
/// <param name="CanOpen">Можно ли открыть задание в банке.</param>
/// <param name="UsedInControl">Использовалось ли задание в платформенном контрольном сценарии.</param>
/// <param name="Blockers">Полный список причин запрета.</param>
public sealed record TaskBankReadinessResponse(
    bool CanOpen,
    bool UsedInControl,
    IReadOnlyList<TaskBankBlockerResponse> Blockers);
