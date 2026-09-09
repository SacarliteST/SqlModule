using Microsoft.Extensions.Options;
using Shouldly;
using SQLModule.Sandbox;
using SQLModule.Sandbox.Dialects;

namespace SQLModule.IntegrationTests.Sandbox;

[Trait("Category", "Docker")]
public sealed class SandboxExecutorTests
{
    private static TestcontainersSandboxExecutor CreateExecutor(int startTimeoutSeconds = 120) =>
        new(new SqlDialectFactory([new PostgresDialect(), new MySqlDialect()]),
            Options.Create(new SandboxOptions
            {
                ContainerStartupTimeoutSeconds = startTimeoutSeconds,
                DefaultQueryTimeoutSeconds = 30,
                MaxRows = 100,
            }));

    private static SandboxDbmsSpec PostgresSpec(string image = "postgres:latest") =>
        new(SystemName: "postgres",
            DockerImage: image,
            DefaultPort: 5432,
            EnvUserKey: "POSTGRES_USER",
            DefaultUsername: "user",
            EnvPasswordKey: "POSTGRES_PASSWORD",
            DefaultPassword: "pass",
            EnvDatabaseKey: "POSTGRES_DB",
            DefaultDatabase: "testdb",
            ExtraEnvConfig: null);

    private static SandboxDbmsSpec UnsupportedSpec() =>
        new(SystemName: "oracle",
            DockerImage: "oracle/database:latest",
            DefaultPort: 1521,
            EnvUserKey: "ORACLE_USER",
            DefaultUsername: "user",
            EnvPasswordKey: "ORACLE_PWD",
            DefaultPassword: "pass",
            EnvDatabaseKey: null,
            DefaultDatabase: "testdb",
            ExtraEnvConfig: null);

    [Fact(DisplayName = "RunAsync → Postgres: setup + SELECT возвращают строки")]
    public async Task RunAsync_Postgres_SetupAndSelect_ReturnsSucceededResult()
    {
        var executor = CreateExecutor();
        var setup = new SandboxSetup([
            "CREATE TABLE cities (id SERIAL PRIMARY KEY, name TEXT NOT NULL)",
            "INSERT INTO cities (name) VALUES ('Moscow'), ('SPb')",
        ]);
        var query = new SandboxQuery("SELECT id, name FROM cities ORDER BY id", 30, 100);

        var result = await executor.RunAsync(PostgresSpec(), setup, query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var rs = result.Value!;
        rs.Succeeded.ShouldBeTrue();
        rs.Columns.ShouldBe(["id", "name"]);
        rs.RowCount.ShouldBe(2);
        rs.Rows[0][1].ShouldBe("Moscow");
        rs.Rows[1][1].ShouldBe("SPb");
    }

    [Fact(DisplayName = "RunAsync → неподдерживаемая СУБД возвращает UnsupportedDbms")]
    public async Task RunAsync_UnsupportedDbms_ReturnsUnsupportedDbmsError()
    {
        var executor = CreateExecutor();
        var result = await executor.RunAsync(
            UnsupportedSpec(), new SandboxSetup([]), new SandboxQuery("SELECT 1", 10, 10), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Sandbox.UnsupportedDbms");
    }

    [Fact(DisplayName = "ValidateSetupAsync → плохой DDL возвращает SetupFailed")]
    public async Task ValidateSetupAsync_BadSetupSql_ReturnsSetupFailedError()
    {
        var executor = CreateExecutor();
        var setup = new SandboxSetup(["NOT VALID SQL @@@@"]);

        var result = await executor.ValidateSetupAsync(PostgresSpec(), setup, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Sandbox.SetupFailed");
    }

    [Fact(DisplayName = "RunAsync → SQL-ошибка в запросе → Succeeded=false внутри Result.Success")]
    public async Task RunAsync_BadQuerySql_ReturnsQueryResultWithSucceededFalse()
    {
        var executor = CreateExecutor();
        var result = await executor.RunAsync(
            PostgresSpec(),
            new SandboxSetup([]),
            new SandboxQuery("SELECT * FROM nonexistent_table_xyz", 10, 10),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Succeeded.ShouldBeFalse();
        result.Value!.Error.ShouldNotBeNullOrEmpty();
    }

    [Fact(DisplayName = "RunAsync → MaxRows ограничивает количество строк")]
    public async Task RunAsync_MaxRowsLimitsOutput()
    {
        var executor = CreateExecutor();
        var setup = new SandboxSetup([
            "CREATE TABLE nums (n INT)",
            "INSERT INTO nums SELECT generate_series(1,10)",
        ]);
        var query = new SandboxQuery("SELECT n FROM nums ORDER BY n", 30, 3);

        var result = await executor.RunAsync(PostgresSpec(), setup, query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.RowCount.ShouldBe(3);
        result.Value.IsTruncated.ShouldBeTrue();
    }
}
