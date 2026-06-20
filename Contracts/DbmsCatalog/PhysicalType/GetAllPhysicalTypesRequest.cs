namespace SQLModule.Contracts.DbmsCatalog.PhysicalType;

/// <summary>Запрос на получение списка физических типов данных с пагинацией и фильтрацией.</summary>
/// <param name="Offset">Смещение (количество пропускаемых записей).</param>
/// <param name="Limit">Максимальное количество возвращаемых записей.</param>
/// <param name="DbmsId">Фильтр по идентификатору СУБД.</param>
public record GetAllPhysicalTypesRequest(int Offset = 0, int Limit = 20, Guid? DbmsId = null);
