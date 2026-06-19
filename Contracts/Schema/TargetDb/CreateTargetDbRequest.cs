namespace SQLModule.Contracts.Schema.TargetDb;

/// <summary>Запрос на создание целевой БД-песочницы.</summary>
/// <param name="DbmsId">Идентификатор СУБД из справочника (<c>DbmsDictionary.Id</c>).</param>
/// <param name="DbName">Уникальное имя создаваемой БД-песочницы.</param>
/// <param name="Description">Необязательное описание назначения БД.</param>
/// <param name="IsReadOnly">Если <c>true</c> — запросы к БД выполняются только на чтение.</param>
public record CreateTargetDbRequest(
    Guid DbmsId,
    string DbName,
    string? Description,
    bool IsReadOnly);
