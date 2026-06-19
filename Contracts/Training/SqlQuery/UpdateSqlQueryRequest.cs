namespace SQLModule.Contracts.Training.SqlQuery;

/// <summary>Запрос на обновление эталонного SQL-запроса.</summary>
/// <param name="QueryText">Новый текст SQL-запроса (не пустой).</param>
/// <param name="StrictColumnOrder">Если <c>true</c> — порядок колонок в ответе обязателен.</param>
/// <param name="StrictRowOrder">Если <c>true</c> — порядок строк в ответе обязателен.</param>
public record UpdateSqlQueryRequest(string QueryText, bool StrictColumnOrder, bool StrictRowOrder);
