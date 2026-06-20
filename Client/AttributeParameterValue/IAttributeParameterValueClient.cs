using SQLModule.Contracts.Schema.AttributeParameterValue;

namespace SQLModule.Client.AttributeParameterValue;

/// <summary>Типизированный клиент для работы со значениями параметров атрибутов.</summary>
public interface IAttributeParameterValueClient
    : ICrudClient<CreateAttributeParameterValueRequest, UpdateAttributeParameterValueRequest,
        AttributeParameterValueResponse>;
