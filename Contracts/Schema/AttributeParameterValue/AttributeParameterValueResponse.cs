namespace SQLModule.Contracts.Schema.AttributeParameterValue;

/// <summary>Ответ с данными значения параметра атрибута.</summary>
/// <param name="Id">Идентификатор значения параметра.</param>
/// <param name="MetaAttributeId">Идентификатор мета-атрибута (колонки).</param>
/// <param name="ParameterDefinitionId">Идентификатор определения параметра физического типа.</param>
/// <param name="ParameterValue">Значение параметра.</param>
/// <param name="CreatedById">Идентификатор пользователя, создавшего запись.</param>
/// <param name="CreatedAt">Дата и время создания записи.</param>
/// <param name="UpdatedById">Идентификатор пользователя, последним обновившего запись.</param>
/// <param name="UpdatedAt">Дата и время последнего обновления записи.</param>
public record AttributeParameterValueResponse(
    Guid Id,
    Guid MetaAttributeId,
    Guid ParameterDefinitionId,
    string ParameterValue,
    Guid CreatedById,
    DateTimeOffset CreatedAt,
    Guid UpdatedById,
    DateTimeOffset UpdatedAt);
