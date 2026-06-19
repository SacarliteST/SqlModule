using SQLModule.Domain.Common;
using SQLModule.Domain.Schema;

namespace SQLModule.Domain.DbmsCatalog;

/// <summary>Определение параметра конфигурации физического типа данных.</summary>
public sealed class ParameterDefinition : AuditableEntity
{
    public Guid PhysicalTypeId { get; private set; }
    public string ParameterKey { get; private set; }
    public string DisplayName { get; private set; }
    public string InputType { get; private set; }
    public string? DefaultValue { get; private set; }
    public short SortOrder { get; private set; }
    public string SqlFragment { get; private set; }
    public bool IsRequired { get; private set; }
    public string? ValuePrefix { get; private set; }
    public string? ValueSuffix { get; private set; }
    public string? Separator { get; private set; }

    public PhysicalType PhysicalType { get; private set; } = default!;

    private readonly List<AttributeParameterValue> attributeParameterValues = [];
    public IReadOnlyCollection<AttributeParameterValue> AttributeParameterValues => attributeParameterValues.AsReadOnly();

    private ParameterDefinition(
        Guid id, Guid physicalTypeId,
        string parameterKey, string displayName, string inputType,
        string? defaultValue, short sortOrder, string sqlFragment, bool isRequired,
        string? valuePrefix, string? valueSuffix, string? separator) : base(id)
    {
        PhysicalTypeId = physicalTypeId;
        ParameterKey = parameterKey;
        DisplayName = displayName;
        InputType = inputType;
        DefaultValue = defaultValue;
        SortOrder = sortOrder;
        SqlFragment = sqlFragment;
        IsRequired = isRequired;
        ValuePrefix = valuePrefix;
        ValueSuffix = valueSuffix;
        Separator = separator;
    }

    public static ParameterDefinition Create(
        Guid physicalTypeId,
        string parameterKey, string displayName, string inputType,
        string? defaultValue, short sortOrder, string sqlFragment, bool isRequired,
        string? valuePrefix, string? valueSuffix, string? separator,
        Guid? id = null)
        => new(id ?? Guid.NewGuid(), physicalTypeId,
            parameterKey, displayName, inputType,
            defaultValue, sortOrder, sqlFragment, isRequired,
            valuePrefix, valueSuffix, separator);

    public void Update(
        string parameterKey, string displayName, string inputType,
        string? defaultValue, short sortOrder, string sqlFragment, bool isRequired,
        string? valuePrefix, string? valueSuffix, string? separator)
    {
        ParameterKey = parameterKey;
        DisplayName = displayName;
        InputType = inputType;
        DefaultValue = defaultValue;
        SortOrder = sortOrder;
        SqlFragment = sqlFragment;
        IsRequired = isRequired;
        ValuePrefix = valuePrefix;
        ValueSuffix = valueSuffix;
        Separator = separator;
    }
}
