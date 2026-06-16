namespace SQLModule.Domain;

/// <summary>Описание мета-колонки (атрибута) в мета-таблице.</summary>
public sealed class MetaAttribute : AuditableEntity
{
    public Guid MetaTableId { get; private set; }
    public Guid PhysicalTypeId { get; private set; }
    public string AttributeName { get; private set; }
    public bool IsPrimaryKey { get; private set; }
    public bool IsRequired { get; private set; }
    public short SortOrder { get; private set; }

    public MetaTable MetaTable { get; private set; } = default!;
    public PhysicalType PhysicalType { get; private set; } = default!;

    private readonly List<CellValue> cellValues = [];
    public IReadOnlyCollection<CellValue> CellValues => cellValues.AsReadOnly();

    private MetaAttribute(Guid id, Guid metaTableId, Guid physicalTypeId,
        string attributeName, bool isPrimaryKey, bool isRequired, short sortOrder) : base(id)
    {
        MetaTableId = metaTableId;
        PhysicalTypeId = physicalTypeId;
        AttributeName = attributeName;
        IsPrimaryKey = isPrimaryKey;
        IsRequired = isRequired;
        SortOrder = sortOrder;
    }

    public static MetaAttribute Create(
        Guid metaTableId, Guid physicalTypeId,
        string attributeName, bool isPrimaryKey, bool isRequired, short sortOrder,
        Guid? id = null)
        => new(id ?? Guid.NewGuid(), metaTableId, physicalTypeId, attributeName, isPrimaryKey, isRequired, sortOrder);

    public void Update(string attributeName, bool isPrimaryKey, bool isRequired, short sortOrder)
    {
        AttributeName = attributeName;
        IsPrimaryKey = isPrimaryKey;
        IsRequired = isRequired;
        SortOrder = sortOrder;
    }
}
