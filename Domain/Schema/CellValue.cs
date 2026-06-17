using SQLModule.Domain.Common;

namespace SQLModule.Domain.Schema;

/// <summary>Значение конкретной ячейки в EAV-модели.</summary>
public sealed class CellValue : AuditableEntity
{
    public Guid DataRecordId { get; private set; }
    public Guid MetaAttributeId { get; private set; }
    public string? TextValue { get; private set; }

    public DataRecord DataRecord { get; private set; } = default!;
    public MetaAttribute MetaAttribute { get; private set; } = default!;

    private CellValue(Guid id, Guid dataRecordId, Guid metaAttributeId, string? textValue) : base(id)
    {
        DataRecordId = dataRecordId;
        MetaAttributeId = metaAttributeId;
        TextValue = textValue;
    }

    public static CellValue Create(Guid dataRecordId, Guid metaAttributeId, string? textValue, Guid? id = null)
        => new(id ?? Guid.NewGuid(), dataRecordId, metaAttributeId, textValue);

    public void Update(string? textValue) => TextValue = textValue;
}
