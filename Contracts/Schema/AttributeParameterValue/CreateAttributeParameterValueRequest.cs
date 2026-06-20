namespace SQLModule.Contracts.Schema.AttributeParameterValue;

/// <summary>Запрос на создание значения параметра атрибута.</summary>
/// <param name="MetaAttributeId">Идентификатор мета-атрибута (колонки), которому принадлежит значение.</param>
/// <param name="ParameterDefinitionId">Идентификатор определения параметра физического типа.</param>
/// <param name="ParameterValue">Значение параметра (не более 500 символов).</param>
public record CreateAttributeParameterValueRequest(
    Guid MetaAttributeId,
    Guid ParameterDefinitionId,
    string ParameterValue);
