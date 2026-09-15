namespace SQLModule.Domain.Training.Validation;

/// <summary>Внутренняя граница многодиалектного AST-анализатора.</summary>
public interface ISqlSyntaxAnalyzer
{
    SqlAnalyzerDialect Dialect { get; }
    string AnalyzerVersion { get; }
    IReadOnlySet<SqlConstruct> SupportedConstructs { get; }

    SqlSyntaxAnalysis Analyze(string sql);
}

/// <summary>Выбирает AST-анализатор по нормализованному system name СУБД.</summary>
public interface ISqlSyntaxAnalyzerResolver
{
    ISqlSyntaxAnalyzer? Resolve(string dbmsSystemName);
}
