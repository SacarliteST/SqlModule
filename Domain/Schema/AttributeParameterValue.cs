using SQLModule.Domain.Common;
using SQLModule.Domain.DbmsCatalog;

namespace SQLModule.Domain.Schema;

/// <summary>Значение параметра, применённое к конкретному мета-атрибуту.</summary>
public sealed class AttributeParameterValue : AuditableEntity
{
    public Guid MetaAttributeId { get; private set; }
    public Guid ParameterDefinitionId { get; private set; }
    public string ParameterValue { get; private set; }

    public MetaAttribute MetaAttribute { get; private set; } = default!;
    public ParameterDefinition ParameterDefinition { get; private set; } = default!;

    private AttributeParameterValue(Guid id, Guid metaAttributeId, Guid parameterDefinitionId, string parameterValue)
        : base(id)
    {
        MetaAttributeId = metaAttributeId;
        ParameterDefinitionId = parameterDefinitionId;
        ParameterValue = parameterValue;
    }

    public static AttributeParameterValue Create(Guid metaAttributeId, Guid parameterDefinitionId, string parameterValue, Guid? id = null)
        => new(id ?? Guid.NewGuid(), metaAttributeId, parameterDefinitionId, parameterValue);

    /// <summary>Создаёт значение без привязки к атрибуту (Id и MetaAttributeId заполнит EF Core).</summary>
    public static AttributeParameterValue CreateInternal(string parameterValue, Guid parameterDefinitionId)
        => new(Guid.Empty, Guid.Empty, parameterDefinitionId, parameterValue);

    public void Update(string parameterValue) => ParameterValue = parameterValue;
}
