namespace SQLModule.Contracts.Schema.MetaTable;

/// <summary>Запрос на создание мета-таблицы.</summary>
/// <param name="TargetDbId">Идентификатор целевой БД, которой принадлежит таблица.</param>
/// <param name="TableName">Название таблицы (не пустое, максимум 200 символов).</param>
/// <param name="Description">Описание таблицы (необязательно, максимум 1000 символов).</param>
public record CreateMetaTableRequest(Guid TargetDbId, string TableName, string? Description);
