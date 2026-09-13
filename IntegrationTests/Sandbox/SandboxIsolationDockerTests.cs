using System.Data.Common;
using DotNet.Testcontainers.Builders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using SQLModule.Common.Results;
using SQLModule.Sandbox;
using SQLModule.Sandbox.Dialects;
using SQLModule.Sandbox.Pooling;

namespace SQLModule.IntegrationTests.Sandbox;

[Trait("Category", "Docker")]
public sealed class SandboxIsolationDockerTests
{
    public static TheoryData<string> SupportedDbms => new() { "postgres", "mysql" };

    [Theory(DisplayName = "Pooled executor: все операции переиспользуют один worker")]
    [MemberData(nameof(SupportedDbms))]
    public async Task PooledExecutor_ReusesWorkerForAllOperations(string systemName)
    {
        var (dbms, _) = CreateProfile(systemName);
        var sandboxOptions = new SandboxOptions
        {
            DefaultQueryTimeoutSeconds = 30,
            Pool = new SandboxPoolOptions
            {
                Enabled = true,
                AcquireTimeoutSeconds = 30,
                PreparationTimeoutSeconds = 30,
                CleanupTimeoutSeconds = 30,
                ShutdownTimeoutSeconds = 30,
                HealthCheckIntervalSeconds = 30,
                RestartBackoffMaxSeconds = 30,
            },
        };
        sandboxOptions.Pool.Profiles.Add(
            systemName,
            new SandboxPoolProfileOptions { MinSize = 0, MaxSize = 1 });
        var options = Options.Create(sandboxOptions);
        var dialectFactory = new SqlDialectFactory([new PostgresDialect(), new MySqlDialect()]);
        var realFactory = new TestcontainersSandboxWorkerFactory(
            dialectFactory,
            options,
            new SandboxPoolInstance(),
            NullLogger<TestcontainersSandboxWorkerFactory>.Instance);
        var workerFactory = new TrackingWorkerFactory(realFactory);
        var leaseManager = new LocalSandboxLeaseManager(
            workerFactory,
            options,
            NullLogger<LocalSandboxLeaseManager>.Instance);
        var isolationManager = new SandboxIsolationManager(
            dialectFactory,
            options,
            NullLogger<SandboxIsolationManager>.Instance);
        var executor = new PooledSandboxExecutor(
            dialectFactory,
            leaseManager,
            isolationManager,
            options,
            NullLogger<PooledSandboxExecutor>.Instance);

        try
        {
            var validation = await executor.ValidateSetupAsync(
                dbms,
                new SandboxSetup(["CREATE TABLE validation_table (id INT NOT NULL)"]),
                CancellationToken.None);
            var inspection = await executor.InspectDdlAsync(
                dbms,
                "CREATE TABLE inspected_table (id INT NOT NULL PRIMARY KEY)",
                CancellationToken.None);
            var execution = await executor.RunAsync(
                dbms,
                new SandboxSetup([
                    "CREATE TABLE query_table (id INT NOT NULL)",
                    "INSERT INTO query_table VALUES (7)",
                ]),
                new SandboxQuery("SELECT id FROM query_table", 30, 10),
                CancellationToken.None);

            validation.IsSuccess.ShouldBeTrue();
            inspection.IsSuccess.ShouldBeTrue();
            inspection.Value!.Tables.ShouldContain(table => table.Name == "inspected_table");
            execution.IsSuccess.ShouldBeTrue();
            execution.Value!.Succeeded.ShouldBeTrue();
            execution.Value.Rows.Single().Single().ShouldBe("7");
            workerFactory.CreateCount.ShouldBe(1);
        }
        finally
        {
            await leaseManager.DrainAsync(CancellationToken.None);
        }
    }

    [Theory(DisplayName = "Pool isolation: runner читает только свою БД и не выполняет DDL/DML")]
    [MemberData(nameof(SupportedDbms))]
    public async Task Runner_IsReadOnlyAndCannotAccessAnotherNamespace(string systemName)
    {
        var (dbms, dialect) = CreateProfile(systemName);
        await using var container = new ContainerBuilder()
            .WithImage(dbms.DockerImage)
            .WithPortBinding(dbms.DefaultPort, true)
            .WithEnvironment(SandboxContainerEnvironment.Build(dbms))
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(dbms.DefaultPort))
            .Build();
        await container.StartAsync();

        var host = container.Hostname;
        var port = container.GetMappedPublicPort(dbms.DefaultPort);
        var first = SandboxIsolationNamespace.Create();
        var second = SandboxIsolationNamespace.Create();
        var firstCreated = false;
        var secondCreated = false;
        try
        {
            await using (var control = await OpenControlAsync(dialect, dbms, host, port))
            {
                await dialect.CreateIsolationNamespaceAsync(control, first, CancellationToken.None);
                firstCreated = true;
                await dialect.CreateIsolationNamespaceAsync(control, second, CancellationToken.None);
                secondCreated = true;
            }

            await ApplySetupAsync(dialect, host, port, first, 101);
            await ApplySetupAsync(dialect, host, port, second, 202);

            (await ReadValueAsync(dialect, host, port, first)).ShouldBe(101);
            (await ReadValueAsync(dialect, host, port, second)).ShouldBe(202);
            await AssertCommandDeniedAsync(dialect, host, port, first, "INSERT INTO lease_data VALUES (303)");
            await AssertCommandDeniedAsync(dialect, host, port, first, "CREATE TABLE forbidden_table (id INT)");
            await AssertOtherNamespaceDeniedAsync(dialect, host, port, first, second);
        }
        finally
        {
            if (firstCreated)
            {
                await CleanupAsync(dialect, dbms, host, port, first);
            }

            if (secondCreated)
            {
                await CleanupAsync(dialect, dbms, host, port, second);
            }
        }
    }

    [Fact(DisplayName = "Pool recovery: SQL-ошибка сохраняет worker, cancellation заменяет его")]
    public async Task PooledExecutor_QuarantinesWorkerOnlyAfterUncertainFailure()
    {
        await using var harness = CreatePooledHarness("postgres");

        var badSetup = await harness.Executor.ValidateSetupAsync(
            harness.Dbms,
            new SandboxSetup(["CREATE TABLE broken ("]),
            CancellationToken.None);
        var afterBadSetup = await harness.Executor.RunAsync(
            harness.Dbms,
            new SandboxSetup(["CREATE TABLE healthy_after_error (id INT)"]),
            new SandboxQuery("SELECT COUNT(*) FROM healthy_after_error", 10, 10),
            CancellationToken.None);

        badSetup.IsSuccess.ShouldBeFalse();
        afterBadSetup.IsSuccess.ShouldBeTrue();
        afterBadSetup.Value!.Succeeded.ShouldBeTrue();
        harness.WorkerFactory.CreateCount.ShouldBe(1);

        var timedOut = await harness.Executor.RunAsync(
            harness.Dbms,
            new SandboxSetup([]),
            new SandboxQuery("SELECT pg_sleep(10)", 1, 10),
            CancellationToken.None);
        var afterTimeout = await harness.Executor.RunAsync(
            harness.Dbms,
            new SandboxSetup(["CREATE TABLE healthy_after_timeout (id INT)"]),
            new SandboxQuery("SELECT COUNT(*) FROM healthy_after_timeout", 10, 10),
            CancellationToken.None);

        timedOut.IsSuccess.ShouldBeTrue();
        timedOut.Value!.Succeeded.ShouldBeFalse();
        afterTimeout.IsSuccess.ShouldBeTrue();
        afterTimeout.Value!.Succeeded.ShouldBeTrue();
        harness.WorkerFactory.CreateCount.ShouldBe(2);

        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));
        try
        {
            await harness.Executor.RunAsync(
                harness.Dbms,
                new SandboxSetup([]),
                new SandboxQuery("SELECT pg_sleep(10)", 15, 10),
                cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            // Ожидаемая реакция провайдера на отмену активного запроса.
        }

        var afterCancellation = await harness.Executor.RunAsync(
            harness.Dbms,
            new SandboxSetup(["CREATE TABLE healthy_after_cancel (id INT)"]),
            new SandboxQuery("SELECT COUNT(*) FROM healthy_after_cancel", 10, 10),
            CancellationToken.None);

        afterCancellation.IsSuccess.ShouldBeTrue();
        afterCancellation.Value!.Succeeded.ShouldBeTrue();
        harness.WorkerFactory.CreateCount.ShouldBe(3);
    }

    [Fact(DisplayName = "Pool recovery: preparation timeout возвращает ошибку и заменяет worker")]
    public async Task PooledExecutor_PreparationTimeoutReplacesWorker()
    {
        await using var harness = CreatePooledHarness("postgres", preparationTimeoutSeconds: 1);

        var timedOut = await harness.Executor.ValidateSetupAsync(
            harness.Dbms,
            new SandboxSetup(["SELECT pg_sleep(10)"]),
            CancellationToken.None);
        var afterTimeout = await harness.Executor.ValidateSetupAsync(
            harness.Dbms,
            new SandboxSetup(["CREATE TABLE healthy_after_timeout (id INT)"]),
            CancellationToken.None);

        timedOut.IsSuccess.ShouldBeFalse();
        timedOut.Error!.Code.ShouldBe("Sandbox.PreparationTimeout");
        afterTimeout.IsSuccess.ShouldBeTrue();
        harness.WorkerFactory.CreateCount.ShouldBe(2);
    }

    private static async Task ApplySetupAsync(
        ISqlDialect dialect,
        string host,
        int port,
        SandboxIsolationNamespace sandboxNamespace,
        int value)
    {
        await using var setup = dialect.CreateConnection(dialect.BuildConnectionString(
            host,
            port,
            sandboxNamespace.DatabaseName,
            sandboxNamespace.SetupUsername,
            sandboxNamespace.SetupPassword));
        await setup.OpenAsync();
        await ExecuteAsync(setup, "CREATE TABLE lease_data (value INT NOT NULL)");
        await ExecuteAsync(setup, $"INSERT INTO lease_data VALUES ({value})");
        await dialect.GrantRunnerAccessAsync(setup, sandboxNamespace, CancellationToken.None);
    }

    private static async Task<int> ReadValueAsync(
        ISqlDialect dialect,
        string host,
        int port,
        SandboxIsolationNamespace sandboxNamespace)
    {
        await using var runner = await OpenRunnerAsync(dialect, host, port, sandboxNamespace);
        await dialect.BeginReadOnlyAsync(runner, CancellationToken.None);
        await using var command = runner.CreateCommand();
        command.CommandText = "SELECT value FROM lease_data";
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static async Task AssertCommandDeniedAsync(
        ISqlDialect dialect,
        string host,
        int port,
        SandboxIsolationNamespace sandboxNamespace,
        string sql)
    {
        await using var runner = await OpenRunnerAsync(dialect, host, port, sandboxNamespace);
        await dialect.BeginReadOnlyAsync(runner, CancellationToken.None);
        await Should.ThrowAsync<DbException>(async () => await ExecuteAsync(runner, sql));
    }

    private static async Task AssertOtherNamespaceDeniedAsync(
        ISqlDialect dialect,
        string host,
        int port,
        SandboxIsolationNamespace credentials,
        SandboxIsolationNamespace target)
    {
        await using var connection = dialect.CreateConnection(dialect.BuildConnectionString(
            host,
            port,
            target.DatabaseName,
            credentials.RunnerUsername,
            credentials.RunnerPassword));
        await Should.ThrowAsync<DbException>(async () => await connection.OpenAsync());
    }

    private static async Task<DbConnection> OpenRunnerAsync(
        ISqlDialect dialect,
        string host,
        int port,
        SandboxIsolationNamespace sandboxNamespace)
    {
        var connection = dialect.CreateConnection(dialect.BuildConnectionString(
            host,
            port,
            sandboxNamespace.DatabaseName,
            sandboxNamespace.RunnerUsername,
            sandboxNamespace.RunnerPassword));
        await connection.OpenAsync();
        return connection;
    }

    private static async Task<DbConnection> OpenControlAsync(
        ISqlDialect dialect,
        SandboxDbmsSpec dbms,
        string host,
        int port)
    {
        var connection = dialect.CreateConnection(dialect.BuildControlConnectionString(host, port, dbms));
        await connection.OpenAsync();
        return connection;
    }

    private static async Task CleanupAsync(
        ISqlDialect dialect,
        SandboxDbmsSpec dbms,
        string host,
        int port,
        SandboxIsolationNamespace sandboxNamespace)
    {
        await using var control = await OpenControlAsync(dialect, dbms, host, port);
        await dialect.CleanupIsolationNamespaceAsync(control, sandboxNamespace, CancellationToken.None);
        (await dialect.IsolationNamespaceExistsAsync(control, sandboxNamespace, CancellationToken.None))
            .ShouldBeFalse();
    }

    private static async Task ExecuteAsync(DbConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private static (SandboxDbmsSpec Dbms, ISqlDialect Dialect) CreateProfile(string systemName) =>
        systemName == "postgres"
            ? (new SandboxDbmsSpec(
                    "postgres", "postgres:15-alpine", 5432,
                    "POSTGRES_USER", "pool_admin", "POSTGRES_PASSWORD", "pool_admin_password",
                    "POSTGRES_DB", "pool_control", null),
                new PostgresDialect())
            : (new SandboxDbmsSpec(
                    "mysql", "mysql:8.0", 3306,
                    "MYSQL_USER", "pool_health", "MYSQL_PASSWORD", "pool_health_password",
                    "MYSQL_DATABASE", "pool_control", "MYSQL_ROOT_PASSWORD=pool_root_password"),
                new MySqlDialect());

    private static PooledHarness CreatePooledHarness(
        string systemName,
        int preparationTimeoutSeconds = 30)
    {
        var (dbms, _) = CreateProfile(systemName);
        var sandboxOptions = new SandboxOptions
        {
            DefaultQueryTimeoutSeconds = 30,
            Pool = new SandboxPoolOptions
            {
                Enabled = true,
                AcquireTimeoutSeconds = 30,
                PreparationTimeoutSeconds = preparationTimeoutSeconds,
                CleanupTimeoutSeconds = 30,
                ShutdownTimeoutSeconds = 30,
                HealthCheckIntervalSeconds = 30,
                RestartBackoffMaxSeconds = 30,
            },
        };
        sandboxOptions.Pool.Profiles.Add(
            systemName,
            new SandboxPoolProfileOptions { MinSize = 0, MaxSize = 1 });
        var options = Options.Create(sandboxOptions);
        var dialectFactory = new SqlDialectFactory([new PostgresDialect(), new MySqlDialect()]);
        var realFactory = new TestcontainersSandboxWorkerFactory(
            dialectFactory,
            options,
            new SandboxPoolInstance(),
            NullLogger<TestcontainersSandboxWorkerFactory>.Instance);
        var workerFactory = new TrackingWorkerFactory(realFactory);
        var leaseManager = new LocalSandboxLeaseManager(
            workerFactory,
            options,
            NullLogger<LocalSandboxLeaseManager>.Instance);
        var isolationManager = new SandboxIsolationManager(
            dialectFactory,
            options,
            NullLogger<SandboxIsolationManager>.Instance);
        var executor = new PooledSandboxExecutor(
            dialectFactory,
            leaseManager,
            isolationManager,
            options,
            NullLogger<PooledSandboxExecutor>.Instance);
        return new PooledHarness(dbms, executor, leaseManager, workerFactory);
    }

    private sealed class TrackingWorkerFactory(ISandboxWorkerFactory inner) : ISandboxWorkerFactory
    {
        private int createCount;

        internal int CreateCount => Volatile.Read(ref createCount);

        public async ValueTask<Result<SandboxWorker>> CreateAsync(
            SandboxWorkerProfile profile,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref createCount);
            return await inner.CreateAsync(profile, cancellationToken);
        }

        public ValueTask<Result> DeleteAsync(
            SandboxWorker worker,
            CancellationToken cancellationToken) => inner.DeleteAsync(worker, cancellationToken);

        public ValueTask<bool> IsHealthyAsync(
            SandboxWorker worker,
            CancellationToken cancellationToken) => inner.IsHealthyAsync(worker, cancellationToken);
    }

    private sealed class PooledHarness(
        SandboxDbmsSpec dbms,
        PooledSandboxExecutor executor,
        LocalSandboxLeaseManager leaseManager,
        TrackingWorkerFactory workerFactory) : IAsyncDisposable
    {
        internal SandboxDbmsSpec Dbms { get; } = dbms;
        internal PooledSandboxExecutor Executor { get; } = executor;
        internal TrackingWorkerFactory WorkerFactory { get; } = workerFactory;

        public async ValueTask DisposeAsync() =>
            await leaseManager.DrainAsync(CancellationToken.None);
    }
}
