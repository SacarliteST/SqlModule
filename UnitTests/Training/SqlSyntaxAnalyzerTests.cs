using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Domain.Training;
using SQLModule.Domain.Training.Validation;
using SQLModule.SqlAnalysis;

namespace SQLModule.UnitTests.Training;

public sealed class SqlSyntaxAnalyzerTests
{
    [Fact]
    public void PostgreSql_ShouldAnalyzeSupportedConstructsAndPhysicalTables()
    {
        var analyzer = Create(SqlAnalyzerDialect.PostgreSql);
        const string sql = """
            WITH paid_orders AS (
                SELECT o.customer_id, o.amount
                FROM sales.orders AS o
                WHERE o.status = 'paid'
            )
            SELECT DISTINCT c.id,
                   SUM(p.amount) OVER (PARTITION BY c.id) AS total
            FROM public.customers AS c
            INNER JOIN paid_orders AS p ON p.customer_id = c.id
            LEFT JOIN public.regions AS r ON r.id = c.region_id
            RIGHT JOIN public.countries AS co ON co.id = r.country_id
            FULL JOIN public.timezones AS tz ON tz.id = co.timezone_id
            WHERE EXISTS (SELECT 1 FROM audit.events AS e WHERE e.customer_id = c.id)
            GROUP BY c.id
            HAVING COUNT(*) > 0
            """;

        var result = analyzer.Analyze(sql);

        result.Status.ShouldBe(SqlSyntaxAnalysisStatus.Succeeded);
        result.IsReadOnly.ShouldBeTrue();
        result.StatementCount.ShouldBe(1);
        result.Constructs.OrderBy(construct => construct)
            .ShouldBe(analyzer.SupportedConstructs.OrderBy(construct => construct));
        result.ReferencedTables.ShouldBe(
            [
                new SqlTableReference("sales", "orders"),
                new SqlTableReference("public", "customers"),
                new SqlTableReference("public", "regions"),
                new SqlTableReference("public", "countries"),
                new SqlTableReference("public", "timezones"),
                new SqlTableReference("audit", "events")
            ],
            ignoreOrder: true);
        result.ReferencedTables.ShouldNotContain(table => table.Name == "paid_orders");
    }

    [Fact]
    public void PostgreSql_ShouldSupportQuotedIdentifiersAndCast()
    {
        var result = Create(SqlAnalyzerDialect.PostgreSql).Analyze(
            "SELECT \"Order Id\"::text FROM \"Sales\".\"Order Items\"");

        result.Status.ShouldBe(SqlSyntaxAnalysisStatus.Succeeded);
        result.ReferencedTables.ShouldBe([new SqlTableReference("Sales", "Order Items")]);
    }

    [Fact]
    public void MySql_ShouldSupportBackticksAndLimit()
    {
        var result = Create(SqlAnalyzerDialect.MySql).Analyze(
            "SELECT `Order Id` FROM `Sales`.`Order Items` LIMIT 10");

        result.Status.ShouldBe(SqlSyntaxAnalysisStatus.Succeeded);
        result.ReferencedTables.ShouldBe([new SqlTableReference("Sales", "Order Items")]);
    }

    [Fact]
    public void MySql_ShouldAnalyzeProvenStructuralCorpus()
    {
        var analyzer = Create(SqlAnalyzerDialect.MySql);
        const string sql = """
            WITH totals AS (
                SELECT customer_id, SUM(amount) AS amount
                FROM `sales`.`orders`
                GROUP BY customer_id
            )
            SELECT DISTINCT c.id,
                   SUM(t.amount) OVER (PARTITION BY c.id) AS total
            FROM `customers` AS c
            INNER JOIN totals AS t ON t.customer_id = c.id
            LEFT JOIN `regions` AS r ON r.id = c.region_id
            RIGHT JOIN `countries` AS co ON co.id = r.country_id
            WHERE c.id IN (SELECT customer_id FROM `vip_customers`)
            GROUP BY c.id
            HAVING COUNT(*) > 0
            LIMIT 5, 10
            """;

        var result = analyzer.Analyze(sql);

        result.Status.ShouldBe(SqlSyntaxAnalysisStatus.Succeeded);
        result.Constructs.OrderBy(construct => construct)
            .ShouldBe(analyzer.SupportedConstructs.OrderBy(construct => construct));
        result.ReferencedTables.ShouldNotContain(table => table.Name == "totals");
        result.ReferencedTables.ShouldContain(new SqlTableReference("sales", "orders"));
        result.ReferencedTables.ShouldContain(new SqlTableReference(null, "vip_customers"));
        analyzer.SupportedConstructs.ShouldNotContain(SqlConstruct.FullJoin);
    }

    [Fact]
    public void KeywordsInCommentsAndStrings_ShouldNotCreateConstructs()
    {
        var result = Create(SqlAnalyzerDialect.PostgreSql).Analyze(
            "SELECT 'JOIN GROUP BY HAVING DISTINCT OVER' AS text FROM users -- JOIN ignored");

        result.Status.ShouldBe(SqlSyntaxAnalysisStatus.Succeeded);
        result.Constructs.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("SELECT FROM", "SqlAnalysis.InvalidSql")]
    [InlineData("SELECT 'unterminated", "SqlAnalysis.InvalidSql")]
    [InlineData("", "SqlAnalysis.EmptySql")]
    public void InvalidSql_ShouldReturnControlledError(string sql, string expectedCode)
    {
        var result = Create(SqlAnalyzerDialect.PostgreSql).Analyze(sql);

        result.Status.ShouldBe(SqlSyntaxAnalysisStatus.InvalidSql);
        result.ErrorCode.ShouldBe(expectedCode);
        result.PublicError.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void MultipleStatements_ShouldBeRejected()
    {
        var result = Create(SqlAnalyzerDialect.PostgreSql).Analyze("SELECT 1; SELECT 2");

        result.Status.ShouldBe(SqlSyntaxAnalysisStatus.UnsupportedSyntax);
        result.ErrorCode.ShouldBe("SqlAnalysis.SingleStatementRequired");
        result.StatementCount.ShouldBe(2);
    }

    [Theory]
    [InlineData("UPDATE users SET name = 'x'")]
    [InlineData("CREATE TABLE users(id integer)")]
    [InlineData("DELETE FROM users")]
    public void MutatingStatement_ShouldBeRejected(string sql)
    {
        var result = Create(SqlAnalyzerDialect.PostgreSql).Analyze(sql);

        result.Status.ShouldBe(SqlSyntaxAnalysisStatus.UnsupportedSyntax);
        result.IsReadOnly.ShouldBeFalse();
        result.ErrorCode.ShouldBe("SqlAnalysis.ReadOnlyQueryRequired");
    }

    [Fact]
    public void Resolver_ShouldResolveKnownAliasesAndIgnoreUnknownDbms()
    {
        var services = new ServiceCollection().AddSqlAnalysis().BuildServiceProvider();
        var resolver = services.GetRequiredService<ISqlSyntaxAnalyzerResolver>();

        resolver.Resolve("postgres")!.Dialect.ShouldBe(SqlAnalyzerDialect.PostgreSql);
        resolver.Resolve(" PostgreSQL ")!.Dialect.ShouldBe(SqlAnalyzerDialect.PostgreSql);
        resolver.Resolve("MYSQL")!.Dialect.ShouldBe(SqlAnalyzerDialect.MySql);
        resolver.Resolve("sql-server").ShouldBeNull();
    }

    private static ISqlSyntaxAnalyzer Create(SqlAnalyzerDialect dialect) =>
        new SqlParserSyntaxAnalyzer(dialect);
}
