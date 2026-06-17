using SQLModule.Domain.Common;

namespace SQLModule.Domain.Schema;

/// <summary>Якорь строки данных в модели EAV.</summary>
public sealed class DataRecord : AuditableEntity
{
    public Guid MetaTableId { get; private set; }
    public int? SortOrder { get; private set; }

    public MetaTable MetaTable { get; private set; } = default!;

    private readonly List<CellValue> cellValues = [];
    public IReadOnlyCollection<CellValue> CellValues => cellValues.AsReadOnly();

    private DataRecord(Guid id, Guid metaTableId, int? sortOrder) : base(id)
    {
        MetaTableId = metaTableId;
        SortOrder = sortOrder;
    }

    public static DataRecord Create(Guid metaTableId, int? sortOrder = null, Guid? id = null)
        => new(id ?? Guid.NewGuid(), metaTableId, sortOrder);

    public void Update(int? sortOrder) => SortOrder = sortOrder;
}
