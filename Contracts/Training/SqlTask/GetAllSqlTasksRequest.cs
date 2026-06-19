namespace SQLModule.Contracts.Training.SqlTask;

/// <summary>Параметры запроса списка SQL-заданий с пагинацией.</summary>
/// <param name="Offset">Количество пропускаемых записей. Минимум: 0. По умолчанию: 0.</param>
/// <param name="Limit">Максимальное количество записей на странице. Диапазон: 1–100. По умолчанию: 20.</param>
public record GetAllSqlTasksRequest(int Offset = 0, int Limit = 20);
