namespace SQLModule.Contracts.Training.SqlTask;

/// <summary>Итог пакетного изменения видимости заданий банка.</summary>
/// <param name="UpdatedCount">Число успешно изменённых заданий.</param>
/// <param name="SkippedCount">Число пропущенных заданий.</param>
/// <param name="Items">Результаты по каждому заданию.</param>
public sealed record TaskBankBatchResponse(
    int UpdatedCount,
    int SkippedCount,
    IReadOnlyList<TaskBankBatchItemResponse> Items);
