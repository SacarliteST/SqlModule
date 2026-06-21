namespace SQLModule.Contracts.Schema.SchemaBuilder;

/// <summary>Значение параметра физического типа для колонки-черновика.</summary>
/// <param name="ParameterDefinitionId">Идентификатор определения параметра.</param>
/// <param name="Value">Значение параметра (например, «255» для VARCHAR(255)).</param>
public record ColumnParameterDraft(
    Guid ParameterDefinitionId,
    string Value);
