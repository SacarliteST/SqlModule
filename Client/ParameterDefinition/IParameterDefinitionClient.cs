using SQLModule.Contracts.DbmsCatalog.ParameterDefinition;

namespace SQLModule.Client.ParameterDefinition;

/// <summary>Типизированный клиент для работы с определениями параметров физических типов данных.</summary>
public interface IParameterDefinitionClient
    : ICrudClient<CreateParameterDefinitionRequest, UpdateParameterDefinitionRequest, ParameterDefinitionResponse>;
