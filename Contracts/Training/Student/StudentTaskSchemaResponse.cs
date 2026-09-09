namespace SQLModule.Contracts.Training.Student;

/// <summary>Безопасное представление структуры учебной базы для студента.</summary>
public sealed record StudentTaskSchemaResponse(
    string DatabaseName,
    string Dbms,
    IReadOnlyList<StudentSchemaTableResponse> Tables,
    IReadOnlyList<StudentSchemaForeignKeyResponse> ForeignKeys);

public sealed record StudentSchemaTableResponse(
    Guid Id,
    string Name,
    string? Description,
    IReadOnlyList<StudentSchemaColumnResponse> Columns);

public sealed record StudentSchemaColumnResponse(
    Guid Id,
    string Name,
    string DataType,
    bool IsNullable,
    bool IsPrimaryKey);

public sealed record StudentSchemaForeignKeyResponse(
    Guid Id,
    string Name,
    Guid FromTableId,
    Guid ToTableId,
    IReadOnlyList<StudentSchemaColumnPairResponse> ColumnPairs);

public sealed record StudentSchemaColumnPairResponse(Guid FromColumnId, Guid ToColumnId);
