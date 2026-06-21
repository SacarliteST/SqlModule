namespace SQLModule.Sandbox;

/// <summary>Описание связи (Foreign Key) в нейтральной модели схемы.</summary>
/// <param name="Name">Название ограничения FK.</param>
/// <param name="SourceColumnKey">Ключ колонки, содержащей Foreign Key.</param>
/// <param name="TargetColumnKey">Ключ колонки, на которую ссылается Foreign Key.</param>
/// <param name="DeleteRule">Действие при удалении (CASCADE, RESTRICT, SET NULL, NO ACTION) или <c>null</c>.</param>
/// <param name="UpdateRule">Действие при обновлении или <c>null</c>.</param>
public sealed record RelationshipSpec(
    string Name,
    string SourceColumnKey,
    string TargetColumnKey,
    string? DeleteRule,
    string? UpdateRule);
