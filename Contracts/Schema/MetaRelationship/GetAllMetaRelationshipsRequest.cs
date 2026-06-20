namespace SQLModule.Contracts.Schema.MetaRelationship;

/// <summary>Параметры постраничного получения списка связей.</summary>
/// <param name="Offset">Смещение (число пропускаемых записей). Минимум 0.</param>
/// <param name="Limit">Максимальное число записей в ответе. От 1 до 100.</param>
/// <param name="AttributeId">
/// Опциональный фильтр: если задан, возвращаются только связи, в которых данный
/// мета-атрибут участвует в роли SourceAttribute или TargetAttribute.
/// </param>
public record GetAllMetaRelationshipsRequest(
    int Offset = 0,
    int Limit = 20,
    Guid? AttributeId = null);
