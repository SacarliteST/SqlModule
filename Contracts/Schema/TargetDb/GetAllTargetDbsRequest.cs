namespace SQLModule.Contracts.Schema.TargetDb;

/// <summary>Параметры запроса списка целевых БД с пагинацией.</summary>
/// <param name="Offset">Количество пропускаемых записей. Минимум: 0. По умолчанию: 0.</param>
/// <param name="Limit">Максимальное количество записей на странице. Диапазон: 1–100. По умолчанию: 20.</param>
public record GetAllTargetDbsRequest(int Offset = 0, int Limit = 20);
