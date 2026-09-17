using SqlParser;
using SqlParser.Ast;
using SqlParser.Dialects;
using SQLModule.Domain.Training;
using SQLModule.Domain.Training.Validation;

namespace SQLModule.SqlAnalysis;

public sealed class SqlParserSyntaxAnalyzer : ISqlSyntaxAnalyzer
{
    private static readonly IReadOnlySet<SqlConstruct> PostgreSqlConstructs = new HashSet<SqlConstruct>(
        Enum.GetValues<SqlConstruct>());
    private static readonly IReadOnlySet<SqlConstruct> MySqlConstructs = new HashSet<SqlConstruct>(
        Enum.GetValues<SqlConstruct>().Where(construct => construct != SqlConstruct.FullJoin));

    private readonly Dialect parserDialect;

    public SqlParserSyntaxAnalyzer(SqlAnalyzerDialect dialect)
    {
        Dialect = dialect;
        parserDialect = dialect switch
        {
            SqlAnalyzerDialect.PostgreSql => new PostgreSqlDialect(),
            SqlAnalyzerDialect.MySql => new MySqlDialect(),
            _ => throw new ArgumentOutOfRangeException(nameof(dialect), dialect, null)
        };
    }

    public SqlAnalyzerDialect Dialect { get; }

    public string AnalyzerVersion => typeof(SqlQueryParser).Assembly.GetName().Version?.ToString(3) ?? "unknown";

    public IReadOnlySet<SqlConstruct> SupportedConstructs => Dialect switch
    {
        SqlAnalyzerDialect.PostgreSql => PostgreSqlConstructs,
        SqlAnalyzerDialect.MySql => MySqlConstructs,
        _ => throw new InvalidOperationException($"Неизвестный SQL-диалект: {Dialect}.")
    };

    public SqlSyntaxAnalysis Analyze(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return Failure(
                SqlSyntaxAnalysisStatus.InvalidSql,
                "SqlAnalysis.EmptySql",
                "SQL-запрос не должен быть пустым.");
        }

        try
        {
            var statements = new SqlQueryParser().Parse(sql.AsSpan(), parserDialect, new ParserOptions());
            if (statements.Count != 1)
            {
                return Failure(
                    SqlSyntaxAnalysisStatus.UnsupportedSyntax,
                    "SqlAnalysis.SingleStatementRequired",
                    "Разрешён ровно один SQL-запрос.",
                    statements.Count);
            }

            if (statements[0] is not Statement.Select select)
            {
                return Failure(
                    SqlSyntaxAnalysisStatus.UnsupportedSyntax,
                    "SqlAnalysis.ReadOnlyQueryRequired",
                    "Разрешены только запросы на чтение данных.",
                    statements.Count);
            }

            var visitor = new AnalysisVisitor();
            visitor.BeginRoot(select.Query);
            ((IElement)select.Query).Visit(visitor);
            visitor.EndRoot(select.Query);

            return new SqlSyntaxAnalysis(
                SqlSyntaxAnalysisStatus.Succeeded,
                visitor.Constructs,
                visitor.ReferencedTables,
                statements.Count,
                true,
                null,
                null);
        }
        catch (Exception exception) when (exception is ParserException or TokenizeException)
        {
            return Failure(
                SqlSyntaxAnalysisStatus.InvalidSql,
                "SqlAnalysis.InvalidSql",
                "SQL-запрос содержит синтаксическую ошибку.");
        }
        catch
        {
            return Failure(
                SqlSyntaxAnalysisStatus.InfrastructureFailure,
                "SqlAnalysis.ParserFailure",
                "Не удалось проверить SQL-запрос.");
        }
    }

    private static SqlSyntaxAnalysis Failure(
        SqlSyntaxAnalysisStatus status,
        string errorCode,
        string publicError,
        int statementCount = 0) =>
        new(
            status,
            new HashSet<SqlConstruct>(),
            [],
            statementCount,
            false,
            errorCode,
            publicError);

    private sealed class AnalysisVisitor : Visitor
    {
        private readonly HashSet<SqlConstruct> constructs = [];
        private readonly List<SqlTableReference> referencedTables = [];
        private readonly Stack<HashSet<string>> cteScopes = [];

        internal IReadOnlySet<SqlConstruct> Constructs => constructs;
        internal IReadOnlyList<SqlTableReference> ReferencedTables => referencedTables;

        internal void BeginRoot(Query query) => PreVisitQuery(query);

        internal void EndRoot(Query query) => PostVisitQuery(query);

        public override ControlFlow PreVisitQuery(Query query)
        {
            var aliases = cteScopes.Count == 0
                ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(cteScopes.Peek(), StringComparer.OrdinalIgnoreCase);

            if (query.With is not null)
            {
                constructs.Add(SqlConstruct.Cte);
                foreach (var cte in query.With.CteTables)
                {
                    aliases.Add(cte.Alias.Name.Value);
                }
            }

            cteScopes.Push(aliases);

            if (query.Body is SetExpression.SelectExpression selectExpression)
            {
                InspectSelect(selectExpression.Select);
            }

            return ControlFlow.Continue;
        }

        public override ControlFlow PostVisitQuery(Query query)
        {
            cteScopes.Pop();
            return ControlFlow.Continue;
        }

        public override ControlFlow PreVisitTableFactor(TableFactor tableFactor)
        {
            if (tableFactor is TableFactor.Derived)
            {
                constructs.Add(SqlConstruct.Subquery);
                return ControlFlow.Continue;
            }

            if (tableFactor is not TableFactor.Table table || table.Name.Values.Count == 0)
            {
                return ControlFlow.Continue;
            }

            var identifiers = table.Name.Values.Select(identifier => identifier.Value).ToArray();
            var tableName = identifiers[^1];
            if (identifiers.Length == 1 &&
                cteScopes.TryPeek(out var aliases) &&
                aliases.Contains(tableName))
            {
                return ControlFlow.Continue;
            }

            var schema = identifiers.Length > 1
                ? string.Join('.', identifiers[..^1])
                : null;
            var reference = new SqlTableReference(schema, tableName);
            if (!referencedTables.Any(existing =>
                    string.Equals(existing.Schema, reference.Schema, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(existing.Name, reference.Name, StringComparison.OrdinalIgnoreCase)))
            {
                referencedTables.Add(reference);
            }

            return ControlFlow.Continue;
        }

        public override ControlFlow PreVisitExpression(Expression expression)
        {
            if (expression is Expression.Subquery or Expression.InSubquery or Expression.Exists)
            {
                constructs.Add(SqlConstruct.Subquery);
            }

            if (expression is Expression.Function { Over: not null })
            {
                constructs.Add(SqlConstruct.WindowFunction);
            }

            return ControlFlow.Continue;
        }

        private void InspectSelect(Select select)
        {
            if (select.Distinct is not null)
            {
                constructs.Add(SqlConstruct.Distinct);
            }

            if (select.GroupBy is GroupByExpression.All ||
                select.GroupBy is GroupByExpression.Expressions { ColumnNames.Count: > 0 })
            {
                constructs.Add(SqlConstruct.GroupBy);
            }

            if (select.Having is not null)
            {
                constructs.Add(SqlConstruct.Having);
            }

            foreach (var source in select.From ?? [])
            {
                foreach (var join in source.Joins ?? [])
                {
                    constructs.Add(SqlConstruct.Join);
                    switch (join.JoinOperator)
                    {
                        case JoinOperator.Inner:
                            constructs.Add(SqlConstruct.InnerJoin);
                            break;
                        case JoinOperator.LeftOuter:
                            constructs.Add(SqlConstruct.LeftJoin);
                            break;
                        case JoinOperator.RightOuter:
                            constructs.Add(SqlConstruct.RightJoin);
                            break;
                        case JoinOperator.FullOuter:
                            constructs.Add(SqlConstruct.FullJoin);
                            break;
                    }
                }
            }
        }
    }
}
