namespace SQLModule.Contracts.Training.SqlQuery;

/// <summary>Запрос на создание эталонного SQL-запроса.</summary>
/// <param name="TargetDbId">Идентификатор целевой БД (датасет для выполнения запроса).</param>
/// <param name="QueryText">Текст SQL-запроса (не пустой).</param>
/// <param name="StrictColumnOrder">Если <c>true</c> — порядок колонок в ответе обязателен.</param>
/// <param name="StrictRowOrder">Если <c>true</c> — порядок строк в ответе обязателен.</param>
public record CreateSqlQueryRequest(
    Guid TargetDbId,
    string QueryText,
    bool StrictColumnOrder,
    bool StrictRowOrder);
