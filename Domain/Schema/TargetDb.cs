using SQLModule.Domain.Common;
using SQLModule.Domain.DbmsCatalog;

namespace SQLModule.Domain.Schema;

/// <summary>Целевая база данных (песочница) для выполнения запросов.</summary>
public sealed class TargetDb : AuditableEntity
{
    public Guid DbmsId { get; private set; }
    public string DbName { get; private set; }
    public string? Description { get; private set; }
    public bool IsReadOnly { get; private set; }
    public long SchemaVersion { get; private set; }

    public DbmsDictionary Dbms { get; private set; } = default!;

    private readonly List<MetaTable> metaTables = [];
    public IReadOnlyCollection<MetaTable> MetaTables => metaTables.AsReadOnly();

    private TargetDb(Guid id, Guid dbmsId, string dbName, string? description, bool isReadOnly) : base(id)
    {
        DbmsId = dbmsId;
        DbName = dbName;
        Description = description;
        IsReadOnly = isReadOnly;
        SchemaVersion = 0;
    }

    public static TargetDb Create(Guid dbmsId, string dbName, string? description, bool isReadOnly, Guid? id = null)
        => new(id ?? Guid.NewGuid(), dbmsId, dbName, description, isReadOnly);

    public void Update(string dbName, string? description, bool isReadOnly)
    {
        DbName = dbName;
        Description = description;
        IsReadOnly = isReadOnly;
    }

    public void IncrementSchemaVersion() => SchemaVersion++;
}
