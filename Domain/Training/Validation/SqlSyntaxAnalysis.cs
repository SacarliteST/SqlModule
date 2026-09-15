namespace SQLModule.Domain.Training.Validation;

/// <summary>Поддерживаемый AST-адаптером SQL-диалект.</summary>
public enum SqlAnalyzerDialect
{
    PostgreSql,
    MySql
}

/// <summary>Категория результата AST-разбора, важная для учёта попыток.</summary>
public enum SqlSyntaxAnalysisStatus
{
    Succeeded,
    InvalidSql,
    UnsupportedSyntax,
    InfrastructureFailure
}

/// <summary>Нормализованная ссылка на физическую таблицу.</summary>
public sealed record SqlTableReference(string? Schema, string Name);

/// <summary>Результат AST-разбора без типов конкретной parser library.</summary>
public sealed record SqlSyntaxAnalysis(
    SqlSyntaxAnalysisStatus Status,
    IReadOnlySet<SqlConstruct> Constructs,
    IReadOnlyList<SqlTableReference> ReferencedTables,
    int StatementCount,
    bool IsReadOnly,
    string? ErrorCode,
    string? PublicError)
{
    public bool IsSuccess => Status == SqlSyntaxAnalysisStatus.Succeeded;
}
