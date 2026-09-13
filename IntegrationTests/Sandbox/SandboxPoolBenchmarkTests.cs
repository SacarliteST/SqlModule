using System.Collections.Concurrent;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using SQLModule.Common.Results;
using SQLModule.Sandbox;
using SQLModule.Sandbox.Dialects;
using SQLModule.Sandbox.Pooling;
using Xunit.Abstractions;

namespace SQLModule.IntegrationTests.Sandbox;

/// <summary>
/// Opt-in нагрузочные проверки тёплого пула. Обычный прогон тестов не запускает контейнеры.
/// </summary>
[Trait("Category", Category)]
public sealed class SandboxPoolBenchmarkTests(ITestOutputHelper output)
{
    private const string Category = "SandboxPoolBenchmark";
    private const string EnabledVariable = "SQLMODULE_SANDBOX_POOL_BENCHMARK";
    private const string IterationsVariable = "SQLMODULE_SANDBOX_POOL_BENCHMARK_ITERATIONS";
    private const string SoakEnabledVariable = "SQLMODULE_SANDBOX_POOL_SOAK";
    private const string SoakMinutesVariable = "SQLMODULE_SANDBOX_POOL_SOAK_MINUTES";
    private const int DefaultIterations = 100;
    private const int DefaultSoakMinutes = 30;

    private static readonly SandboxSetup Dataset = new([
        "CREATE TABLE departments (id INTEGER PRIMARY KEY, name VARCHAR(100) NOT NULL)",
        "CREATE TABLE employees (id INTEGER PRIMARY KEY, department_id INTEGER NOT NULL, name VARCHAR(100) NOT NULL, salary DECIMAL(10, 2) NOT NULL, CONSTRAINT fk_employees_departments FOREIGN KEY (department_id) REFERENCES departments(id))",
        "INSERT INTO departments (id, name) VALUES (1, 'Engineering'), (2, 'Support')",
        "INSERT INTO employees (id, department_id, name, salary) VALUES (1, 1, 'Alice', 120000.00), (2, 1, 'Bob', 90000.00), (3, 2, 'Carol', 70000.00)",
    ]);

    private const string Ddl =
        "CREATE TABLE departments (id INTEGER PRIMARY KEY, name VARCHAR(100) NOT NULL); " +
        "CREATE TABLE employees (id INTEGER PRIMARY KEY, department_id INTEGER NOT NULL, " +
        "name VARCHAR(100) NOT NULL, salary DECIMAL(10, 2) NOT NULL, " +
        "CONSTRAINT fk_employees_departments FOREIGN KEY (department_id) REFERENCES departments(id));";

    private static readonly SandboxQuery Query = new(
        "SELECT d.name, COUNT(e.id) AS employee_count FROM departments d " +
        "LEFT JOIN employees e ON e.department_id = d.id GROUP BY d.id, d.name ORDER BY d.id",
        30,
        100);

    private static readonly IReadOnlyDictionary<(string Dbms, string Operation), double> BaselineP95 =
        new Dictionary<(string, string), double>
        {
            [("postgres", "RunAsync")] = 4488,
            [("postgres", "ValidateSetupAsync")] = 7626,
            [("postgres", "InspectDdlAsync")] = 5403,
            [("mysql", "RunAsync")] = 20282,
            [("mysql", "ValidateSetupAsync")] = 14451,
            [("mysql", "InspectDdlAsync")] = 14573,
        };

    [Fact(DisplayName = "Pool benchmark: последовательная нагрузка и burst для PostgreSQL и MySQL")]
    public async Task MeasureSequentialAndBurst()
    {
        if (!IsEnabled(EnabledVariable))
        {
            output.WriteLine($"Benchmark отключён. Установите {EnabledVariable}=true для запуска Docker-нагрузки.");
            return;
        }

        var iterations = ReadPositiveInt(IterationsVariable, DefaultIterations);
        WriteEnvironment("benchmark", $"iterationsPerOperation={iterations}");
        var acceptanceFailures = new List<string>();

        foreach (var definition in ProfileDefinitions())
        {
            await using var harness = CreateHarness(definition);
            await harness.WarmAsync();
            await using var resources = new DockerResourceSampler(harness.WorkerFactory);
            await resources.StartAsync();

            foreach (var operation in CreateOperations(harness.Executor, definition.Dbms))
            {
                using var metrics = new PoolMetricsCollector(definition.Dbms.SystemName);
                var measurement = await MeasureAsync(operation.Action, iterations);
                WriteMeasurement(definition, operation.Name, "sequential", measurement, harness, metrics, resources);

                var baselineP95 = BaselineP95[(definition.Dbms.SystemName, operation.Name)];
                var improvement = 100d * (1d - measurement.P95Milliseconds / baselineP95);
                output.WriteLine(String.Format(CultureInfo.InvariantCulture,
                    "comparisonDbms={0}; operation={1}; baselineP95Ms={2:F0}; poolP95Ms={3:F0}; " +
                    "improvementPercent={4:F1}; targetMet={5}",
                    definition.Dbms.SystemName, operation.Name, baselineP95,
                    measurement.P95Milliseconds, improvement, improvement >= 30));
                if (measurement.P95Milliseconds > baselineP95 || improvement < 30)
                {
                    acceptanceFailures.Add(
                        $"{definition.Dbms.SystemName}/{operation.Name}: p95={measurement.P95Milliseconds:F0} мс, " +
                        $"baseline={baselineP95:F0} мс, улучшение={improvement:F1}%.");
                }
            }

            var burstSize = 2 * definition.MaxSize;
            using var burstMetrics = new PoolMetricsCollector(definition.Dbms.SystemName);
            var burst = await MeasureBurstAsync(
                CreateOperations(harness.Executor, definition.Dbms)[0].Action,
                burstSize);
            WriteMeasurement(
                definition,
                "RunAsync",
                $"burst-{burstSize}",
                burst,
                harness,
                burstMetrics,
                resources);

            harness.WorkerFactory.MaximumLiveCount.ShouldBeLessThanOrEqualTo(definition.MaxSize);
            var leftovers = await harness.ReadLeftoversAsync();
            leftovers.ShouldBe(new PoolLeftovers(0, 0, 0));
        }

        acceptanceFailures.ShouldBeEmpty(
            "Все сценарии должны быть измерены до общей проверки целевого улучшения p95.");
    }

    [Fact(DisplayName = "Pool soak: 30 минут без роста контейнеров, namespace и подключений")]
    public async Task RunSoak()
    {
        if (!IsEnabled(EnabledVariable) || !IsEnabled(SoakEnabledVariable))
        {
            output.WriteLine(
                $"Soak отключён. Установите {EnabledVariable}=true и {SoakEnabledVariable}=true для запуска.");
            return;
        }

        var duration = TimeSpan.FromMinutes(ReadPositiveInt(SoakMinutesVariable, DefaultSoakMinutes));
        WriteEnvironment("soak", $"durationMinutes={duration.TotalMinutes:F0}");

        var reports = await Task.WhenAll(ProfileDefinitions()
            .Select(definition => RunProfileSoakAsync(definition, duration)));
        foreach (var report in reports)
        {
            output.WriteLine(report);
        }
    }

    private static async Task<string> RunProfileSoakAsync(
        BenchmarkProfile definition,
        TimeSpan duration)
    {
        await using var harness = CreateHarness(definition);
        await harness.WarmAsync();
        await using var resources = new DockerResourceSampler(harness.WorkerFactory);
        await resources.StartAsync();
        var startedAt = Stopwatch.GetTimestamp();
        var completed = 0;
        var failures = new ConcurrentQueue<string>();

        var workers = Enumerable.Range(0, definition.MaxSize)
            .Select(_ => Task.Run(async () =>
            {
                while (Stopwatch.GetElapsedTime(startedAt) < duration)
                {
                    var result = await harness.Executor.RunAsync(
                        definition.Dbms,
                        Dataset,
                        Query,
                        CancellationToken.None);
                    if (!result.IsSuccess || result.Value is not { Succeeded: true, RowCount: 2 })
                    {
                        failures.Enqueue(result.Error?.Code ?? result.Value?.Error ?? "Неизвестная ошибка");
                    }

                    Interlocked.Increment(ref completed);
                }
            }))
            .ToArray();

        await Task.WhenAll(workers);
        await resources.StopAsync();
        var elapsed = Stopwatch.GetElapsedTime(startedAt);
        var leftovers = await harness.ReadLeftoversAsync();

        failures.ShouldBeEmpty();
        completed.ShouldBeGreaterThan(0);
        harness.WorkerFactory.MaximumLiveCount.ShouldBeLessThanOrEqualTo(definition.MaxSize);
        leftovers.ShouldBe(new PoolLeftovers(0, 0, 0));
        if (resources.FirstMemoryMebibytes is { } first && resources.LastMemoryMebibytes is { } last)
        {
            last.ShouldBeLessThanOrEqualTo(first + 128d * definition.MaxSize);
        }

        return String.Format(CultureInfo.InvariantCulture,
            "mode=soak; dbms={0}; elapsedMinutes={1:F1}; completed={2}; failures={3}; " +
            "throughputPerSecond={4:F3}; containerStarts={5}; maxLiveContainers={6}; " +
            "firstMemoryMiB={7}; lastMemoryMiB={8}; peakMemoryMiB={9}; peakCpuPercent={10}; " +
            "leftoverNamespaces={11}; leftoverUsers={12}; leftoverConnections={13}",
            definition.Dbms.SystemName, elapsed.TotalMinutes, completed, failures.Count,
            completed / elapsed.TotalSeconds, harness.WorkerFactory.CreateCount,
            harness.WorkerFactory.MaximumLiveCount, Format(resources.FirstMemoryMebibytes),
            Format(resources.LastMemoryMebibytes), Format(resources.PeakMemoryMebibytes),
            Format(resources.PeakCpuPercent), leftovers.Namespaces, leftovers.Users,
            leftovers.Connections);
    }

    private static IReadOnlyList<BenchmarkOperation> CreateOperations(
        PooledSandboxExecutor executor,
        SandboxDbmsSpec dbms) =>
    [
        new("RunAsync", async ct =>
        {
            var result = await executor.RunAsync(dbms, Dataset, Query, ct);
            return result.IsSuccess && result.Value is { Succeeded: true, RowCount: 2 };
        }),
        new("ValidateSetupAsync", async ct =>
        {
            var result = await executor.ValidateSetupAsync(dbms, Dataset, ct);
            return result.IsSuccess;
        }),
        new("InspectDdlAsync", async ct =>
        {
            var result = await executor.InspectDdlAsync(dbms, Ddl, ct);
            return result.IsSuccess && result.Value is { Tables.Count: 2, Relationships.Count: 1 };
        }),
    ];

    private static async Task<BenchmarkMeasurement> MeasureAsync(
        Func<CancellationToken, Task<bool>> action,
        int count)
    {
        var durations = new List<double>(count);
        var scenario = Stopwatch.StartNew();
        for (var iteration = 0; iteration < count; iteration++)
        {
            var stopwatch = Stopwatch.StartNew();
            Assert.True(await action(CancellationToken.None), $"Итерация {iteration + 1} завершилась ошибкой.");
            durations.Add(stopwatch.Elapsed.TotalMilliseconds);
        }

        scenario.Stop();
        return BenchmarkMeasurement.Create(durations, scenario.Elapsed);
    }

    private static async Task<BenchmarkMeasurement> MeasureBurstAsync(
        Func<CancellationToken, Task<bool>> action,
        int count)
    {
        using var start = new ManualResetEventSlim();
        var scenario = Stopwatch.StartNew();
        var tasks = Enumerable.Range(0, count)
            .Select(_ => Task.Run(async () =>
            {
                start.Wait();
                var stopwatch = Stopwatch.StartNew();
                var succeeded = await action(CancellationToken.None);
                return (Succeeded: succeeded, Duration: stopwatch.Elapsed.TotalMilliseconds);
            }))
            .ToArray();
        start.Set();
        var results = await Task.WhenAll(tasks);
        scenario.Stop();

        results.ShouldAllBe(result => result.Succeeded);
        return BenchmarkMeasurement.Create(results.Select(result => result.Duration).ToList(), scenario.Elapsed);
    }

    private void WriteMeasurement(
        BenchmarkProfile definition,
        string operation,
        string mode,
        BenchmarkMeasurement measurement,
        BenchmarkHarness harness,
        PoolMetricsCollector metrics,
        DockerResourceSampler resources)
    {
        var leaseWait = metrics.Snapshot("sqlmodule_sandbox_pool_lease_wait_ms");
        output.WriteLine(String.Format(CultureInfo.InvariantCulture,
            "mode={0}; dbms={1}; image={2}; operation={3}; count={4}; p50Ms={5:F0}; p95Ms={6:F0}; " +
            "p99Ms={7:F0}; throughputPerSecond={8:F3}; leaseWaitP95Ms={9}; containerStarts={10}; " +
            "maxLiveContainers={11}; peakContainerCpuPercent={12}; peakContainerMemoryMiB={13}; resourceSamples={14}",
            mode, definition.Dbms.SystemName, definition.Dbms.DockerImage, operation, measurement.Count,
            measurement.P50Milliseconds, measurement.P95Milliseconds, measurement.P99Milliseconds,
            measurement.ThroughputPerSecond, leaseWait.Count == 0 ? "n/a" : Format(Percentile(leaseWait, 0.95)),
            harness.WorkerFactory.CreateCount, harness.WorkerFactory.MaximumLiveCount,
            Format(resources.PeakCpuPercent), Format(resources.PeakMemoryMebibytes), resources.SampleCount));
    }

    private void WriteEnvironment(string mode, string details)
    {
        using var process = Process.GetCurrentProcess();
        output.WriteLine($"startedAtUtc={DateTimeOffset.UtcNow:O}; mode={mode}; {details}");
        output.WriteLine(
            $"machine={Environment.MachineName}; os={Environment.OSVersion}; processors={Environment.ProcessorCount}; " +
            $"runnerWorkingSetMiB={process.WorkingSet64 / 1024d / 1024d:F1}");
    }

    private static BenchmarkHarness CreateHarness(BenchmarkProfile definition)
    {
        var sandboxOptions = new SandboxOptions
        {
            ContainerStartupTimeoutSeconds = 120,
            DefaultQueryTimeoutSeconds = 30,
            MaxRows = 100,
            Pool = new SandboxPoolOptions
            {
                Enabled = true,
                AcquireTimeoutSeconds = 60,
                PreparationTimeoutSeconds = 60,
                CleanupTimeoutSeconds = 30,
                ShutdownTimeoutSeconds = 60,
                HealthCheckIntervalSeconds = 30,
                RestartBackoffMaxSeconds = 30,
            },
        };
        var limits = new SandboxPoolProfileOptions { MinSize = 0, MaxSize = definition.MaxSize };
        sandboxOptions.Pool.Profiles.Add(definition.Dbms.SystemName, limits);
        var options = Options.Create(sandboxOptions);
        var dialectFactory = new SqlDialectFactory([new PostgresDialect(), new MySqlDialect()]);
        var instance = new SandboxPoolInstance();
        var realFactory = new TestcontainersSandboxWorkerFactory(
            dialectFactory,
            options,
            instance,
            NullLogger<TestcontainersSandboxWorkerFactory>.Instance);
        var workerFactory = new BenchmarkWorkerFactory(realFactory);
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
        return new BenchmarkHarness(
            definition,
            executor,
            leaseManager,
            workerFactory,
            dialectFactory);
    }

    private static IReadOnlyList<BenchmarkProfile> ProfileDefinitions() =>
    [
        new(new SandboxDbmsSpec(
            "postgres", "postgres:15-alpine", 5432,
            "POSTGRES_USER", "pool_admin", "POSTGRES_PASSWORD", "pool_admin_password",
            "POSTGRES_DB", "pool_control", null), 2),
        new(new SandboxDbmsSpec(
            "mysql", "mysql:8.0", 3306,
            "MYSQL_USER", "pool_health", "MYSQL_PASSWORD", "pool_health_password",
            "MYSQL_DATABASE", "pool_control", "MYSQL_ROOT_PASSWORD=pool_root_password"), 1),
    ];

    private static bool IsEnabled(string variable) =>
        String.Equals(Environment.GetEnvironmentVariable(variable), "true", StringComparison.OrdinalIgnoreCase);

    private static int ReadPositiveInt(string variable, int fallback) =>
        Int32.TryParse(Environment.GetEnvironmentVariable(variable), CultureInfo.InvariantCulture, out var value) &&
        value > 0
            ? value
            : fallback;

    private static double Percentile(IReadOnlyList<double> values, double percentile)
    {
        var sorted = values.Order().ToArray();
        return sorted[Math.Max(0, (int)Math.Ceiling(percentile * sorted.Length) - 1)];
    }

    private static string Format(double? value) =>
        value?.ToString("F1", CultureInfo.InvariantCulture) ?? "n/a";

    private sealed record BenchmarkProfile(SandboxDbmsSpec Dbms, int MaxSize);
    private sealed record BenchmarkOperation(string Name, Func<CancellationToken, Task<bool>> Action);
    private sealed record PoolLeftovers(int Namespaces, int Users, int Connections);

    private sealed record BenchmarkMeasurement(
        int Count,
        double P50Milliseconds,
        double P95Milliseconds,
        double P99Milliseconds,
        double ThroughputPerSecond)
    {
        internal static BenchmarkMeasurement Create(IReadOnlyList<double> durations, TimeSpan elapsed) => new(
            durations.Count,
            Percentile(durations, 0.50),
            Percentile(durations, 0.95),
            Percentile(durations, 0.99),
            durations.Count / elapsed.TotalSeconds);
    }

    private sealed class BenchmarkHarness(
        BenchmarkProfile definition,
        PooledSandboxExecutor executor,
        LocalSandboxLeaseManager leaseManager,
        BenchmarkWorkerFactory workerFactory,
        ISqlDialectFactory dialectFactory) : IAsyncDisposable
    {
        internal PooledSandboxExecutor Executor { get; } = executor;
        internal BenchmarkWorkerFactory WorkerFactory { get; } = workerFactory;

        internal async Task WarmAsync()
        {
            var result = await Executor.RunAsync(
                definition.Dbms,
                Dataset,
                Query,
                CancellationToken.None);
            Assert.True(result.IsSuccess && result.Value is { Succeeded: true, RowCount: 2 },
                $"Не удалось прогреть профиль {definition.Dbms.SystemName}.");
        }

        internal async Task<PoolLeftovers> ReadLeftoversAsync()
        {
            var workers = WorkerFactory.LiveWorkers;
            var namespaces = 0;
            var users = 0;
            var connections = 0;
            foreach (var worker in workers)
            {
                var dialect = dialectFactory.GetDialectFor(worker.Profile.Dbms.SystemName)!;
                await using var connection = dialect.CreateConnection(dialect.BuildControlConnectionString(
                    worker.Host,
                    worker.MappedPort,
                    worker.Profile.Dbms));
                await connection.OpenAsync();

                if (worker.Profile.Dbms.SystemName == "postgres")
                {
                    namespaces += await ScalarAsync(connection,
                        "SELECT COUNT(*) FROM pg_database WHERE datname LIKE 'sqlm\\_%' ESCAPE '\\'");
                    users += await ScalarAsync(connection,
                        "SELECT COUNT(*) FROM pg_roles WHERE rolname LIKE 'sqlm\\_%' ESCAPE '\\'");
                    connections += await ScalarAsync(connection,
                        "SELECT COUNT(*) FROM pg_stat_activity WHERE datname LIKE 'sqlm\\_%' ESCAPE '\\'");
                }
                else
                {
                    namespaces += await ScalarAsync(connection,
                        "SELECT COUNT(*) FROM information_schema.schemata WHERE schema_name LIKE 'sqlm\\_%'");
                    users += await ScalarAsync(connection,
                        "SELECT COUNT(*) FROM mysql.user WHERE user LIKE 'sqlm\\_%'");
                    connections += await ScalarAsync(connection,
                        "SELECT COUNT(*) FROM information_schema.processlist WHERE db LIKE 'sqlm\\_%'");
                }
            }

            return new PoolLeftovers(namespaces, users, connections);
        }

        public async ValueTask DisposeAsync()
        {
            await leaseManager.DrainAsync(CancellationToken.None);
            WorkerFactory.LiveWorkers.ShouldBeEmpty();
        }

        private static async Task<int> ScalarAsync(DbConnection connection, string sql)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            return Convert.ToInt32(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
        }
    }

    private sealed class BenchmarkWorkerFactory(ISandboxWorkerFactory inner) : ISandboxWorkerFactory
    {
        private readonly ConcurrentDictionary<Guid, SandboxWorker> liveWorkers = new();
        private int createCount;
        private int maximumLiveCount;

        internal int CreateCount => Volatile.Read(ref createCount);
        internal int MaximumLiveCount => Volatile.Read(ref maximumLiveCount);
        internal IReadOnlyCollection<SandboxWorker> LiveWorkers => liveWorkers.Values.ToArray();

        public async ValueTask<Result<SandboxWorker>> CreateAsync(
            SandboxWorkerProfile profile,
            CancellationToken cancellationToken)
        {
            var result = await inner.CreateAsync(profile, cancellationToken);
            if (result.IsSuccess && result.Value is { } worker)
            {
                liveWorkers[worker.WorkerId] = worker;
                Interlocked.Increment(ref createCount);
                UpdateMaximum(liveWorkers.Count);
            }

            return result;
        }

        public async ValueTask<Result> DeleteAsync(
            SandboxWorker worker,
            CancellationToken cancellationToken)
        {
            var result = await inner.DeleteAsync(worker, cancellationToken);
            if (result.IsSuccess)
            {
                liveWorkers.TryRemove(worker.WorkerId, out _);
            }

            return result;
        }

        public ValueTask<bool> IsHealthyAsync(
            SandboxWorker worker,
            CancellationToken cancellationToken) => inner.IsHealthyAsync(worker, cancellationToken);

        private void UpdateMaximum(int candidate)
        {
            var current = Volatile.Read(ref maximumLiveCount);
            while (candidate > current)
            {
                var observed = Interlocked.CompareExchange(ref maximumLiveCount, candidate, current);
                if (observed == current)
                {
                    return;
                }

                current = observed;
            }
        }
    }

    private sealed class PoolMetricsCollector : IDisposable
    {
        private readonly string profile;
        private readonly ConcurrentDictionary<string, ConcurrentQueue<double>> measurements = new();
        private readonly MeterListener listener = new();

        internal PoolMetricsCollector(string profile)
        {
            this.profile = profile;
            listener.InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == SandboxPoolTelemetry.InstrumentationName)
                {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            };
            listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) =>
            {
                if (HasProfile(tags, this.profile))
                {
                    measurements.GetOrAdd(instrument.Name, _ => new ConcurrentQueue<double>()).Enqueue(value);
                }
            });
            listener.Start();
        }

        internal IReadOnlyList<double> Snapshot(string instrument) =>
            measurements.TryGetValue(instrument, out var values) ? values.ToArray() : [];

        public void Dispose() => listener.Dispose();

        private static bool HasProfile(ReadOnlySpan<KeyValuePair<string, object?>> tags, string profile)
        {
            foreach (var tag in tags)
            {
                if (tag.Key == "profile" && String.Equals(tag.Value?.ToString(), profile,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }

    private sealed class DockerResourceSampler(BenchmarkWorkerFactory workerFactory) : IAsyncDisposable
    {
        private readonly CancellationTokenSource stopping = new();
        private Task? samplingTask;

        internal double? FirstMemoryMebibytes { get; private set; }
        internal double? LastMemoryMebibytes { get; private set; }
        internal double? PeakMemoryMebibytes { get; private set; }
        internal double? PeakCpuPercent { get; private set; }
        internal int SampleCount { get; private set; }

        internal Task StartAsync()
        {
            samplingTask = SampleContinuouslyAsync(stopping.Token);
            return Task.CompletedTask;
        }

        internal async Task StopAsync()
        {
            if (stopping.IsCancellationRequested)
            {
                return;
            }

            await stopping.CancelAsync();
            if (samplingTask is not null)
            {
                await samplingTask;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync();
            stopping.Dispose();
        }

        private async Task SampleContinuouslyAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await SampleOnceAsync(ct);
                    await Task.Delay(TimeSpan.FromSeconds(1), ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    return;
                }
                catch
                {
                    // Ресурсные метрики вспомогательные и не должны останавливать benchmark.
                }
            }
        }

        private async Task SampleOnceAsync(CancellationToken ct)
        {
            var totalMemory = 0d;
            var totalCpu = 0d;
            var sampled = 0;
            foreach (var worker in workerFactory.LiveWorkers)
            {
                var stats = await RunDockerAsync(ct, "stats", "--no-stream", "--format",
                    "{{.CPUPerc}}|{{.MemUsage}}", worker.ContainerId);
                var parts = stats.Trim().Split('|');
                if (parts.Length != 2 ||
                    !Double.TryParse(parts[0].TrimEnd('%'), NumberStyles.Float,
                        CultureInfo.InvariantCulture, out var cpu) ||
                    !TryParseMebibytes(parts[1].Split('/')[0].Trim(), out var memory))
                {
                    continue;
                }

                totalCpu += cpu;
                totalMemory += memory;
                sampled++;
            }

            if (sampled == 0)
            {
                return;
            }

            FirstMemoryMebibytes ??= totalMemory;
            LastMemoryMebibytes = totalMemory;
            PeakMemoryMebibytes = Math.Max(PeakMemoryMebibytes ?? 0, totalMemory);
            PeakCpuPercent = Math.Max(PeakCpuPercent ?? 0, totalCpu);
            SampleCount++;
        }

        private static async Task<string> RunDockerAsync(CancellationToken ct, params string[] arguments)
        {
            var startInfo = new ProcessStartInfo("docker")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            foreach (var argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Не удалось запустить docker CLI.");
            var standardOutput = process.StandardOutput.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);
            return await standardOutput;
        }

        private static bool TryParseMebibytes(string value, out double result)
        {
            var units = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["B"] = 1d / 1024d / 1024d,
                ["kB"] = 1000d / 1024d / 1024d,
                ["KiB"] = 1d / 1024d,
                ["MB"] = 1000d * 1000d / 1024d / 1024d,
                ["MiB"] = 1d,
                ["GB"] = 1000d * 1000d * 1000d / 1024d / 1024d,
                ["GiB"] = 1024d,
            };
            foreach (var (unit, multiplier) in units.OrderByDescending(pair => pair.Key.Length))
            {
                if (value.EndsWith(unit, StringComparison.OrdinalIgnoreCase) &&
                    Double.TryParse(value[..^unit.Length], NumberStyles.Float,
                        CultureInfo.InvariantCulture, out var amount))
                {
                    result = amount * multiplier;
                    return true;
                }
            }

            result = 0;
            return false;
        }
    }
}
