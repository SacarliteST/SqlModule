namespace SQLModule.Contracts.DbmsCatalog.PhysicalType;

/// <summary>Запрос на создание физического типа данных.</summary>
/// <param name="DbmsId">Идентификатор СУБД, которой принадлежит тип данных.</param>
/// <param name="TypeName">Название физического типа данных (не более 100 символов).</param>
public record CreatePhysicalTypeRequest(Guid DbmsId, string TypeName);
