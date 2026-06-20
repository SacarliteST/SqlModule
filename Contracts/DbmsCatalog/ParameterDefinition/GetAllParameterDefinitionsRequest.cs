namespace SQLModule.Contracts.DbmsCatalog.ParameterDefinition;

/// <summary>Запрос на получение списка определений параметров с пагинацией и фильтрацией.</summary>
/// <param name="Offset">Смещение (количество пропускаемых записей, ≥ 0).</param>
/// <param name="Limit">Максимальное количество возвращаемых записей (1–100).</param>
/// <param name="PhysicalTypeId">Опциональный фильтр по идентификатору физического типа.</param>
public record GetAllParameterDefinitionsRequest(int Offset = 0, int Limit = 20, Guid? PhysicalTypeId = null);
