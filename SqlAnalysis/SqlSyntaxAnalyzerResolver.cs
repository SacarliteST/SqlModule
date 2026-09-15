using SQLModule.Domain.Training.Validation;

namespace SQLModule.SqlAnalysis;

public sealed class SqlSyntaxAnalyzerResolver(IEnumerable<ISqlSyntaxAnalyzer> analyzers)
    : ISqlSyntaxAnalyzerResolver
{
    private readonly IReadOnlyDictionary<SqlAnalyzerDialect, ISqlSyntaxAnalyzer> analyzers =
        analyzers.ToDictionary(analyzer => analyzer.Dialect);

    public ISqlSyntaxAnalyzer? Resolve(string dbmsSystemName)
    {
        var dialect = dbmsSystemName.Trim().ToLowerInvariant() switch
        {
            "postgres" or "postgresql" => SqlAnalyzerDialect.PostgreSql,
            "mysql" => SqlAnalyzerDialect.MySql,
            _ => (SqlAnalyzerDialect?)null
        };

        return dialect is not null && analyzers.TryGetValue(dialect.Value, out var analyzer)
            ? analyzer
            : null;
    }
}
