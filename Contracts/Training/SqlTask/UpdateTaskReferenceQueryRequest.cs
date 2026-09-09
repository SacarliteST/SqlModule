namespace SQLModule.Contracts.Training.SqlTask;

/// <summary>Полное новое состояние эталонного решения текущего задания.</summary>
/// <param name="TargetDbId">Учебная база для выполнения эталона.</param>
/// <param name="QueryText">Текст эталонного SQL-запроса.</param>
/// <param name="StrictColumnOrder">Требовать совпадение порядка колонок.</param>
/// <param name="StrictRowOrder">Требовать совпадение порядка строк.</param>
public sealed record UpdateTaskReferenceQueryRequest(
    Guid? TargetDbId,
    string? QueryText,
    bool? StrictColumnOrder,
    bool? StrictRowOrder);
