namespace SQLModule.Contracts.Schema.MetaTable;

/// <summary>Параметры запроса страницы мета-таблиц.</summary>
/// <param name="Offset">Количество пропускаемых записей (>= 0). По умолчанию 0.</param>
/// <param name="Limit">Размер страницы (1–100). По умолчанию 20.</param>
/// <param name="TargetDbId">Опциональный фильтр по целевой БД.</param>
public record GetAllMetaTablesRequest(int Offset = 0, int Limit = 20, Guid? TargetDbId = null);
