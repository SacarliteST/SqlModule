namespace SQLModule.Contracts.Schema.SchemaBuilder;

public sealed class TableRowsResponse
{
    public required int Count { get; init; }
    public required int Offset { get; init; }
    public required int Limit { get; init; }
    public required string SchemaVersion { get; init; }
    public required IReadOnlyList<TableRowsColumnResponse> Columns { get; init; }
    public required IReadOnlyList<TableRowResponse> Items { get; init; }
}

public sealed class TableRowsColumnResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string PhysicalTypeName { get; init; }
    public required bool IsRequired { get; init; }
}

public sealed class TableRowResponse
{
    public required Guid Id { get; init; }
    public required string Version { get; init; }
    public required int SortOrder { get; init; }
    public required IReadOnlyDictionary<Guid, TableCellResponse> Cells { get; init; }
}

public sealed class TableCellResponse
{
    public string? Value { get; init; }
    public required bool IsNull { get; init; }
}

public sealed class BatchTableRowsRequest
{
    public string? SchemaVersion { get; init; }
    public IReadOnlyList<TableRowChange>? Changes { get; init; }
}

public enum TableRowOperation { Create, Update, Delete }

public sealed class TableRowChange
{
    public TableRowOperation? Operation { get; init; }
    public Guid? Id { get; init; }
    public string? TempId { get; init; }
    public string? Version { get; init; }
    public int? SortOrder { get; init; }
    public IReadOnlyDictionary<Guid, TableCellRequest>? Cells { get; init; }
}

public sealed class TableCellRequest
{
    public string? Value { get; init; }
    public bool? IsNull { get; init; }
}

public sealed class BatchTableRowsResponse
{
    public required string SchemaVersion { get; init; }
    public required IReadOnlyDictionary<string, Guid> CreatedIds { get; init; }
    public required IReadOnlyList<TableRowResponse> Rows { get; init; }
}
