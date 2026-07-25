namespace SQLModule.Contracts.Training.SqlQuery;

/// <summary>Результат успешной проверки эталонного SQL без сохранения.</summary>
/// <param name="IsValid">Признак успешного выполнения запроса.</param>
/// <param name="Columns">Обнаруженные колонки результата.</param>
/// <param name="SampleRows">Строки preview, ограниченные настройкой Sandbox:MaxRows.</param>
/// <param name="RowCount">Количество строк, возвращённых в preview.</param>
/// <param name="ExecutionTimeMs">Продолжительность выполнения SQL-запроса.</param>
/// <param name="ValidatedAt">Момент завершения проверки.</param>
public sealed record ValidateSqlQueryResponse(
    bool IsValid,
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyList<string?>> SampleRows,
    int RowCount,
    long ExecutionTimeMs,
    DateTimeOffset ValidatedAt);
