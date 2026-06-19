namespace SQLModule.Contracts.Training.Topic;

/// <summary>Параметры запроса списка тем с пагинацией.</summary>
/// <param name="Offset">Количество пропускаемых записей. Минимум: 0. По умолчанию: 0.</param>
/// <param name="Limit">Максимальное количество записей на странице. Диапазон: 1–100. По умолчанию: 20.</param>
public record GetAllTopicsRequest(int Offset = 0, int Limit = 20);
