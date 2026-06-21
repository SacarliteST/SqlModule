namespace SQLModule.Contracts.Schema.SchemaBuilder;

/// <summary>Черновик связи (Foreign Key) между двумя колонками.</summary>
/// <param name="Name">Название ограничения FK.</param>
/// <param name="SourceColumnTempId">TempId колонки, содержащей Foreign Key.</param>
/// <param name="TargetColumnTempId">TempId колонки, на которую ссылается Foreign Key.</param>
/// <param name="DeleteRule">Действие при удалении родительской строки (CASCADE, RESTRICT, SET NULL, NO ACTION) или <c>null</c>.</param>
/// <param name="UpdateRule">Действие при обновлении родительской строки (CASCADE, RESTRICT, SET NULL, NO ACTION) или <c>null</c>.</param>
public record RelationshipDraft(
    string Name,
    string SourceColumnTempId,
    string TargetColumnTempId,
    string? DeleteRule,
    string? UpdateRule);
