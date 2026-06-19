namespace SQLModule.Contracts.Schema.TargetDb;

/// <summary>Запрос на обновление целевой БД-песочницы.</summary>
/// <param name="DbName">Новое имя БД-песочницы.</param>
/// <param name="Description">Новое описание (передайте <c>null</c>, чтобы очистить).</param>
/// <param name="IsReadOnly">Флаг доступа только на чтение.</param>
public record UpdateTargetDbRequest(
    string DbName,
    string? Description,
    bool IsReadOnly);
