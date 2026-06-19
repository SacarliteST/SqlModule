namespace SQLModule.Contracts.Training.SqlQuery;

/// <summary>Параметры запроса списка SQL-запросов с пагинацией.</summary>
/// <param name="Offset">Количество пропускаемых записей. Минимум: 0. По умолчанию: 0.</param>
/// <param name="Limit">Максимальное количество возвращаемых записей. Диапазон: 1–100. По умолчанию: 20.</param>
public record GetAllSqlQueriesRequest(int Offset = 0, int Limit = 20);
