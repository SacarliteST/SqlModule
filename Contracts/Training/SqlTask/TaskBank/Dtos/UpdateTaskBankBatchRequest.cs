namespace SQLModule.Contracts.Training.SqlTask;

/// <summary>Запрос пакетного открытия или закрытия заданий банка.</summary>
/// <param name="TaskIds">Идентификаторы заданий.</param>
/// <param name="Open">true — открыть, false — закрыть.</param>
public sealed record UpdateTaskBankBatchRequest(IReadOnlyList<Guid>? TaskIds, bool? Open);
