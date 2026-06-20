namespace SQLModule.Contracts.Schema.MetaTable;

/// <summary>Запрос на обновление мета-таблицы.</summary>
/// <param name="TableName">Новое название таблицы (не пустое, максимум 200 символов).</param>
/// <param name="Description">Новое описание таблицы (необязательно, максимум 1000 символов).</param>
public record UpdateMetaTableRequest(string TableName, string? Description);
