using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Options;
using SQLModule.Common.Results;
using SQLModule.Sandbox;
using SQLModule.Sandbox.Dialects;
using Xunit.Abstractions;

namespace SQLModule.IntegrationTests.Sandbox;

/// <summary>
/// Воспроизводимый baseline текущего one-shot исполнителя. Тест намеренно opt-in:
/// без SQLMODULE_SANDBOX_BASELINE=true он не запускает контейнеры.
/// </summary>
[Trait("Category", Category)]
public sealed class SandboxOneShotBaselineTests(ITestOutputHelper output)
{
    private const string Category = "SandboxBaseline";
    private const string EnabledVariable = "SQLMODULE_SANDBOX_BASELINE";
    private const string IterationsVariable = "SQLMODULE_SANDBOX_BASELINE_ITERATIONS";
    private const int DefaultIterations = 5;

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

    [Fact(DisplayName = "Baseline one-shot sandbox для PostgreSQL и MySQL")]
    public async Task MeasureCurrentOneShotExecutor()
    {
        if (!String.Equals(Environment.GetEnvironmentVariable(EnabledVariable), "true",
                StringComparison.OrdinalIgnoreCase))
        {
            output.WriteLine($"Baseline отключён. Установите {EnabledVariable}=true для запуска Docker-нагрузки.");
            return;
        }

        var iterations = ReadIterations();
        var executor = CreateExecutor();
        var process = Process.GetCurrentProcess();
        var startedAt = DateTimeOffset.UtcNow;

        output.WriteLine($"startedAtUtc={startedAt:O}");
        output.WriteLine($"machine={Environment.MachineName}; os={Environment.OSVersion}; processors={Environment.ProcessorCount}");
        output.WriteLine($"iterationsPerScenario={iterations}; expectedContainerStarts={iterations * 6}");
        output.WriteLine("dataset=departments(2 rows)+employees(3 rows); query=aggregate employees by department");

        foreach (var dbms in DbmsProfiles())
        {
            await MeasureAsync(dbms, "RunAsync", iterations,
                ct => executor.RunAsync(dbms, Dataset, Query, ct),
                result => result.IsSuccess && result.Value is { Succeeded: true, RowCount: 2 });

            await MeasureAsync(dbms, "ValidateSetupAsync", iterations,
                async ct =>
                {
                    var result = await executor.ValidateSetupAsync(dbms, Dataset, ct);
                    return result.IsSuccess
                        ? Result<bool>.Success(true)
                        : Result<bool>.Fail(result.Error!);
                },
                result => result.IsSuccess);

            await MeasureAsync(dbms, "InspectDdlAsync", iterations,
                ct => executor.InspectDdlAsync(dbms, Ddl, ct),
                result => result.IsSuccess && result.Value is { Tables.Count: 2, Relationships.Count: 1 });
        }

        process.Refresh();
        output.WriteLine($"runnerTotalCpuMs={process.TotalProcessorTime.TotalMilliseconds:F0}; runnerWorkingSetMiB={ToMebibytes(process.WorkingSet64):F1}");
        output.WriteLine($"finishedAtUtc={DateTimeOffset.UtcNow:O}");
    }

    private async Task MeasureAsync<T>(
        SandboxDbmsSpec dbms,
        string operation,
        int iterations,
        Func<CancellationToken, Task<Result<T>>> action,
        Func<Result<T>, bool> isExpected)
    {
        var durations = new List<double>(iterations);
        await using var sampler = new DockerStatsSampler(dbms.DockerImage);
        await sampler.StartAsync();
        var scenario = Stopwatch.StartNew();

        for (var iteration = 0; iteration < iterations; iteration++)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = await action(CancellationToken.None);
            stopwatch.Stop();
            Assert.True(isExpected(result),
                $"{dbms.SystemName}/{operation}, итерация {iteration + 1}: {result.Error?.Code} {result.Error?.Message}");
            durations.Add(stopwatch.Elapsed.TotalMilliseconds);
        }

        scenario.Stop();
        await sampler.StopAsync();

        durations.Sort();
        var throughput = iterations / scenario.Elapsed.TotalSeconds;
        var peakCpu = sampler.SampleCount == 0
            ? "n/a"
            : sampler.PeakCpuPercent.ToString("F2", CultureInfo.InvariantCulture);
        var peakMemory = sampler.SampleCount == 0
            ? "n/a"
            : sampler.PeakMemoryMebibytes.ToString("F1", CultureInfo.InvariantCulture);
        output.WriteLine(String.Format(CultureInfo.InvariantCulture,
            "dbms={0}; image={1}; operation={2}; count={3}; containerStarts={3}; " +
            "p50Ms={4:F0}; p95Ms={5:F0}; p99Ms={6:F0}; throughputPerSecond={7:F3}; " +
            "peakContainerCpuPercent={8}; peakContainerMemoryMiB={9}; resourceSamples={10}",
            dbms.SystemName, dbms.DockerImage, operation, iterations,
            Percentile(durations, 0.50), Percentile(durations, 0.95), Percentile(durations, 0.99), throughput,
            peakCpu, peakMemory, sampler.SampleCount));
    }

    private static TestcontainersSandboxExecutor CreateExecutor() =>
        new(new SqlDialectFactory([new PostgresDialect(), new MySqlDialect()]),
            Options.Create(new SandboxOptions
            {
                ContainerStartupTimeoutSeconds = 120,
                DefaultQueryTimeoutSeconds = 30,
                MaxRows = 100,
            }));

    private static IReadOnlyList<SandboxDbmsSpec> DbmsProfiles() =>
    [
        new("postgres", "postgres:15-alpine", 5432,
            "POSTGRES_USER", "baseline_user", "POSTGRES_PASSWORD", "baseline_pass",
            "POSTGRES_DB", "baseline_db", null),
        new("mysql", "mysql:8.0", 3306,
            "MYSQL_USER", "baseline_user", "MYSQL_PASSWORD", "baseline_pass",
            "MYSQL_DATABASE", "baseline_db", "MYSQL_ROOT_PASSWORD=baseline_root_pass"),
    ];

    private static int ReadIterations()
    {
        var raw = Environment.GetEnvironmentVariable(IterationsVariable);
        return Int32.TryParse(raw, CultureInfo.InvariantCulture, out var parsed) && parsed > 0
            ? parsed
            : DefaultIterations;
    }

    private static double Percentile(IReadOnlyList<double> sortedValues, double percentile)
    {
        var index = Math.Max(0, (int)Math.Ceiling(percentile * sortedValues.Count) - 1);
        return sortedValues[index];
    }

    private static double ToMebibytes(long bytes) => bytes / 1024d / 1024d;

    private sealed class DockerStatsSampler(string image) : IAsyncDisposable
    {
        private readonly CancellationTokenSource stopping = new();
        private Task? samplingTask;

        public double PeakCpuPercent { get; private set; }
        public double PeakMemoryMebibytes { get; private set; }
        public int SampleCount { get; private set; }

        public Task StartAsync()
        {
            samplingTask = SampleContinuouslyAsync(stopping.Token);
            return Task.CompletedTask;
        }

        public async Task StopAsync()
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
                    await Task.Delay(TimeSpan.FromMilliseconds(250), ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    return;
                }
                catch
                {
                    // Метрики ресурсов вспомогательные и не должны ломать функциональный baseline.
                }
            }
        }

        private async Task SampleOnceAsync(CancellationToken ct)
        {
            var containerIds = await RunDockerAsync(ct, "ps", "--filter", $"ancestor={image}", "--format", "{{.ID}}");
            foreach (var containerId in containerIds.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
            {
                var stats = await RunDockerAsync(ct, "stats", "--no-stream", "--format",
                    "{{.CPUPerc}}|{{.MemUsage}}", containerId);
                var parts = stats.Trim().Split('|');
                if (parts.Length != 2)
                {
                    continue;
                }

                if (Double.TryParse(parts[0].TrimEnd('%'), NumberStyles.Float,
                        CultureInfo.InvariantCulture, out var cpu))
                {
                    PeakCpuPercent = Math.Max(PeakCpuPercent, cpu);
                }

                var usedMemory = parts[1].Split('/')[0].Trim();
                if (TryParseMebibytes(usedMemory, out var memory))
                {
                    PeakMemoryMebibytes = Math.Max(PeakMemoryMebibytes, memory);
                }

                SampleCount++;
            }
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

            var outputTask = process.StandardOutput.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);
            return await outputTask;
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
                if (!value.EndsWith(unit, StringComparison.OrdinalIgnoreCase) ||
                    !Double.TryParse(value[..^unit.Length], NumberStyles.Float,
                        CultureInfo.InvariantCulture, out var amount))
                {
                    continue;
                }

                result = amount * multiplier;
                return true;
            }

            result = 0;
            return false;
        }
    }
}
