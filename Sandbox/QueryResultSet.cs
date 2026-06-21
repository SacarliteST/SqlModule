namespace SQLModule.Sandbox;

/// <summary>Результат выполнения SQL-запроса в песочнице.</summary>
public sealed record QueryResultSet(
    bool Succeeded,
    string? Error,
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyList<string?>> Rows,
    int RowCount,
    long DurationMs);
