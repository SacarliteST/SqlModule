namespace SQLModule.Contracts.Schema.AttributeParameterValue;

/// <summary>Запрос на обновление значения параметра атрибута.</summary>
/// <param name="ParameterValue">Новое значение параметра (не более 500 символов).</param>
public record UpdateAttributeParameterValueRequest(string ParameterValue);
