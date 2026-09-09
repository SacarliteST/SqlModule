namespace SQLModule.Contracts.ModuleIntegration;

/// <summary>Опубликованное задание, доступное для привязки в Education.</summary>
/// <param name="Ref">Непрозрачный идентификатор задания во внешнем модуле.</param>
/// <param name="Name">Название задания.</param>
/// <param name="Description">Краткое описание задания.</param>
public sealed record ModuleTaskCatalogItemResponse(
    string Ref,
    string Name,
    string Description);
