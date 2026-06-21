using Shouldly;
using SQLModule.Sandbox;
using SQLModule.Sandbox.Dialects;

namespace SQLModule.UnitTests.Schema;

public sealed class SchemaSqlGeneratorTests
{
    private readonly ISqlSyntax postgres = new PostgresDialect();
    private readonly ISqlSyntax mysql = new MySqlDialect();
    private readonly SchemaSqlGenerator gen = new();

    [Fact]
    public void GenerateDdl_Postgres_QuotesIdentifiersWithDoubleQuotes()
    {
        var schema = SimpleTableSchema("t1", "users", [
            Col("c1", "id", "SERIAL", isPk: true, isNullable: false, order: 0)
        ]);

        var ddl = gen.GenerateDdl(postgres, schema);

        ddl.ShouldHaveSingleItem();
        ddl[0].ShouldContain("\"users\"");
        ddl[0].ShouldContain("\"id\"");
    }

    [Fact]
    public void GenerateDdl_MySql_QuotesIdentifiersWithBackticks()
    {
        var schema = SimpleTableSchema("t1", "users", [
            Col("c1", "id", "INT", isPk: true, isNullable: false, order: 0)
        ]);

        var ddl = gen.GenerateDdl(mysql, schema);

        ddl.ShouldHaveSingleItem();
        ddl[0].ShouldContain("`users`");
        ddl[0].ShouldContain("`id`");
    }

    [Fact]
    public void GenerateDdl_Postgres_EmitsNotNullForRequiredColumns()
    {
        var schema = SimpleTableSchema("t1", "items", [
            Col("c1", "id", "SERIAL", isPk: true, isNullable: false, order: 0),
            Col("c2", "name", "VARCHAR(255)", isPk: false, isNullable: false, order: 1),
            Col("c3", "note", "TEXT", isPk: false, isNullable: true, order: 2)
        ]);

        var ddl = gen.GenerateDdl(postgres, schema);

        ddl[0].ShouldContain("\"name\" VARCHAR(255) NOT NULL");
        ddl[0].ShouldNotContain("\"note\" TEXT NOT NULL");
    }

    [Fact]
    public void GenerateDdl_Postgres_EmitsTableLevelPrimaryKey()
    {
        var schema = SimpleTableSchema("t1", "orders", [
            Col("c1", "id", "INTEGER", isPk: true, isNullable: false, order: 0),
            Col("c2", "seq", "INTEGER", isPk: true, isNullable: false, order: 1)
        ]);

        var ddl = gen.GenerateDdl(postgres, schema);

        ddl[0].ShouldContain("PRIMARY KEY (\"id\", \"seq\")");
    }

    [Fact]
    public void GenerateDdl_Postgres_EmitsAlterTableForForeignKey()
    {
        var schema = new SchemaSpec("db", [
            new TableSpec("t1", "customers", [
                Col("c1", "id", "SERIAL", isPk: true, isNullable: false, order: 0)
            ]),
            new TableSpec("t2", "orders", [
                Col("c2", "id", "SERIAL", isPk: true, isNullable: false, order: 0),
                Col("c3", "customer_id", "INTEGER", isPk: false, isNullable: false, order: 1)
            ])
        ], [
            new RelationshipSpec("fk_orders_cust", "c3", "c1", "CASCADE", "NO ACTION")
        ]);

        var ddl = gen.GenerateDdl(postgres, schema);

        ddl.Count.ShouldBe(3);
        var alter = ddl[2];
        alter.ShouldContain("ALTER TABLE \"orders\"");
        alter.ShouldContain("FOREIGN KEY (\"customer_id\")");
        alter.ShouldContain("REFERENCES \"customers\" (\"id\")");
        alter.ShouldContain("ON DELETE CASCADE");
        alter.ShouldContain("ON UPDATE NO ACTION");
    }

    [Fact]
    public void GenerateDdl_Postgres_OmitsFkRulesWhenNull()
    {
        var schema = new SchemaSpec("db", [
            new TableSpec("t1", "a", [Col("c1", "id", "INT", true, false, 0)]),
            new TableSpec("t2", "b", [
                Col("c2", "id", "INT", true, false, 0),
                Col("c3", "a_id", "INT", false, false, 1)
            ])
        ], [new RelationshipSpec("fk_b_a", "c3", "c1", null, null)]);

        var ddl = gen.GenerateDdl(postgres, schema);

        var alter = ddl[2];
        alter.ShouldNotContain("ON DELETE");
        alter.ShouldNotContain("ON UPDATE");
    }

    // ── INSERT ───────────────────────────────────────────────────────────────

    [Fact]
    public void GenerateInserts_Postgres_FormatsStringValues()
    {
        var schema = SimpleTableSchema("t1", "items", [
            Col("c1", "id", "INTEGER", true, false, 0),
            Col("c2", "name", "VARCHAR(100)", false, true, 1)
        ]);
        var data = new DataSpec([
            new TableRows("t1", [
                new RowSpec([new CellSpec("c1", "1"), new CellSpec("c2", "Alice")])
            ])
        ]);

        var inserts = gen.GenerateInserts(postgres, schema, data);

        inserts.ShouldHaveSingleItem();
        inserts[0].ShouldContain("INSERT INTO \"items\"");
        inserts[0].ShouldContain("1");
        inserts[0].ShouldContain("'Alice'");
    }

    [Fact]
    public void GenerateInserts_Postgres_EscapesSingleQuotesInStrings()
    {
        var schema = SimpleTableSchema("t1", "items", [
            Col("c1", "name", "VARCHAR(200)", false, true, 0)
        ]);
        var data = new DataSpec([
            new TableRows("t1", [new RowSpec([new CellSpec("c1", "O'Brien")])])
        ]);

        var inserts = gen.GenerateInserts(postgres, schema, data);

        inserts[0].ShouldContain("'O''Brien'");
    }

    [Fact]
    public void GenerateInserts_Postgres_FormatsNullAsNull()
    {
        var schema = SimpleTableSchema("t1", "items", [
            Col("c1", "id", "INTEGER", true, false, 0),
            Col("c2", "note", "TEXT", false, true, 1)
        ]);
        var data = new DataSpec([
            new TableRows("t1", [
                new RowSpec([new CellSpec("c1", "42"), new CellSpec("c2", null)])
            ])
        ]);

        var inserts = gen.GenerateInserts(postgres, schema, data);

        inserts[0].ShouldContain("NULL");
        inserts[0].ShouldNotContain("'NULL'");
    }

    [Fact]
    public void GenerateInserts_Postgres_EmitsNumbersWithoutQuotes()
    {
        var schema = SimpleTableSchema("t1", "prices", [
            Col("c1", "amount", "DECIMAL(10,2)", false, false, 0)
        ]);
        var data = new DataSpec([
            new TableRows("t1", [new RowSpec([new CellSpec("c1", "123.45")])])
        ]);

        var inserts = gen.GenerateInserts(postgres, schema, data);

        inserts[0].ShouldContain("123.45");
        inserts[0].ShouldNotContain("'123.45'");
    }

    [Fact]
    public void GenerateInserts_ReferencedTableInsertedBeforeReferencingTable()
    {
        var schema = new SchemaSpec("db", [
            new TableSpec("t1", "customers", [
                Col("c1", "id", "SERIAL", true, false, 0)
            ]),
            new TableSpec("t2", "orders", [
                Col("c2", "id", "SERIAL", true, false, 0),
                Col("c3", "customer_id", "INTEGER", false, false, 1)
            ])
        ], [
            new RelationshipSpec("fk", "c3", "c1", null, null)
        ]);
        var data = new DataSpec([
            new TableRows("t2", [new RowSpec([new CellSpec("c2", "1"), new CellSpec("c3", "10")])]),
            new TableRows("t1", [new RowSpec([new CellSpec("c1", "10")])])
        ]);

        var inserts = gen.GenerateInserts(postgres, schema, data);

        inserts.Count.ShouldBe(2);
        var customersIdx = inserts.ToList().FindIndex(s => s.Contains("customers"));
        var ordersIdx = inserts.ToList().FindIndex(s => s.Contains("orders"));
        customersIdx.ShouldBeLessThan(ordersIdx);
    }

    [Fact]
    public void GenerateInserts_MissingCellDefaultsToNull()
    {
        var schema = SimpleTableSchema("t1", "items", [
            Col("c1", "id", "INTEGER", true, false, 0),
            Col("c2", "name", "VARCHAR(100)", false, true, 1)
        ]);
        var data = new DataSpec([
            new TableRows("t1", [new RowSpec([new CellSpec("c1", "1")])])
        ]);

        var inserts = gen.GenerateInserts(postgres, schema, data);

        inserts[0].ShouldContain("NULL");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static SchemaSpec SimpleTableSchema(string tableKey, string tableName, IReadOnlyList<ColumnSpec> cols) =>
        new("db", [new TableSpec(tableKey, tableName, cols)], []);

    private static ColumnSpec Col(string key, string name, string sqlType,
        bool isPk, bool isNullable, short order) =>
        new(key, name, sqlType, isPk, isNullable, order);
}
