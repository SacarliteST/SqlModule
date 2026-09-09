namespace SQLModule.Sandbox;

public sealed record InspectedSchema(
    IReadOnlyList<InspectedTable> Tables,
    IReadOnlyList<InspectedRelationship> Relationships);

public sealed record InspectedTable(string Name, IReadOnlyList<InspectedColumn> Columns);

public sealed record InspectedColumn(
    string Name,
    string StoreType,
    bool IsNullable,
    bool IsPrimaryKey,
    int SortOrder,
    int? Length,
    int? Precision,
    int? Scale);

public sealed record InspectedRelationship(
    string Name,
    string SourceTable,
    string SourceColumn,
    string TargetTable,
    string TargetColumn,
    string DeleteRule,
    string UpdateRule);
