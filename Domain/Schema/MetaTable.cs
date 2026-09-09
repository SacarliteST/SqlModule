using SQLModule.Domain.Common;

namespace SQLModule.Domain.Schema;

/// <summary>Описание структуры мета-таблицы в целевой БД.</summary>
public sealed class MetaTable : AuditableEntity
{
    public Guid TargetDbId { get; private set; }
    public string TableName { get; private set; }
    public string? Description { get; private set; }
    public short SortOrder { get; private set; }

    public TargetDb TargetDb { get; private set; } = default!;

    private readonly List<MetaAttribute> attributes = [];
    public IReadOnlyCollection<MetaAttribute> Attributes => attributes.AsReadOnly();

    private readonly List<DataRecord> dataRecords = [];
    public IReadOnlyCollection<DataRecord> DataRecords => dataRecords.AsReadOnly();

    private MetaTable(Guid id, Guid targetDbId, string tableName, string? description, short sortOrder) : base(id)
    {
        TargetDbId = targetDbId;
        TableName = tableName;
        Description = description;
        SortOrder = sortOrder;
    }

    public static MetaTable Create(
        Guid targetDbId,
        string tableName,
        string? description,
        Guid? id = null,
        short sortOrder = 0)
        => new(id ?? Guid.NewGuid(), targetDbId, tableName, description, sortOrder);

    public void Update(string tableName, string? description, short? sortOrder = null)
    {
        TableName = tableName;
        Description = description;
        if (sortOrder.HasValue)
        {
            SortOrder = sortOrder.Value;
        }
    }
}
