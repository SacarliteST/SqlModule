namespace SQLModule.Contracts.DbmsCatalog.PhysicalType;

/// <summary>Запрос на обновление физического типа данных.</summary>
/// <param name="TypeName">Новое название физического типа данных (не более 100 символов).</param>
public record UpdatePhysicalTypeRequest(string TypeName);
