using System.Data.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using SQLModule.Common.Results;
using SQLModule.Sandbox;
using SQLModule.Sandbox.Dialects;
using SQLModule.Sandbox.Pooling;

namespace SQLModule.UnitTests.Sandbox;

public sealed class PooledSandboxExecutorTests
{
    [Fact(DisplayName = "Pooled executor: ошибка cleanup выбраковывает worker и заменяет результат")]
    public async Task CleanupFailure_DiscardsWorkerAndReturnsInfrastructureError()
    {
        SandboxLeaseDisposition? releasedAs = null;
        var profile = CreateProfile();
        var leaseManager = new SingleLeaseManager(profile, disposition => releasedAs = disposition);
        var isolationManager = new FailingCleanupIsolationManager();
        var options = CreateOptions();
        var executor = new PooledSandboxExecutor(
            new SqlDialectFactory([new PostgresDialect(), new MySqlDialect()]),
            leaseManager,
            isolationManager,
            options,
            NullLogger<PooledSandboxExecutor>.Instance);

        var result = await executor.ValidateSetupAsync(
            profile.Dbms,
            new SandboxSetup([]),
            CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Sandbox.IsolationCleanupFailed");
        isolationManager.CleanupCalled.ShouldBeTrue();
        releasedAs.ShouldBe(SandboxLeaseDisposition.Discard);
    }

    private static IOptions<SandboxOptions> CreateOptions()
    {
        var options = new SandboxOptions
        {
            Pool = new SandboxPoolOptions { Enabled = true },
        };
        options.Pool.Profiles.Add(
            "postgres",
            new SandboxPoolProfileOptions { MinSize = 0, MaxSize = 1 });
        return Options.Create(options);
    }

    private static SandboxWorkerProfile CreateProfile() =>
        new(
            new SandboxDbmsSpec(
                "postgres",
                "postgres:15-alpine",
                5432,
                "POSTGRES_USER",
                "admin",
                "POSTGRES_PASSWORD",
                "secret",
                "POSTGRES_DB",
                "control",
                null),
            new SandboxPoolProfileOptions { MinSize = 0, MaxSize = 1 });

    private sealed class SingleLeaseManager(
        SandboxWorkerProfile profile,
        Action<SandboxLeaseDisposition> released) : ISandboxLeaseManager
    {
        public ValueTask<Result<SandboxLease>> AcquireAsync(
            SandboxWorkerProfile requestedProfile,
            CancellationToken cancellationToken)
        {
            var worker = new SandboxWorker(profile, "container", "localhost", 5432);
            worker.TryMarkReady();
            worker.TryLease(out var generation);
            var lease = new SandboxLease(
                worker,
                generation,
                value =>
                {
                    released(value.Disposition);
                    return ValueTask.CompletedTask;
                });
            return ValueTask.FromResult(Result<SandboxLease>.Success(lease));
        }
    }

    private sealed class FailingCleanupIsolationManager : ISandboxIsolationManager
    {
        internal bool CleanupCalled { get; private set; }

        public Task<Result<SandboxIsolationNamespace>> CreateAsync(
            SandboxLease lease,
            CancellationToken ct) => Task.FromResult(
            Result<SandboxIsolationNamespace>.Success(SandboxIsolationNamespace.Create()));

        public Task<Result<DbConnection>> OpenSetupConnectionAsync(
            SandboxLease lease,
            SandboxIsolationNamespace sandboxNamespace,
            CancellationToken ct) => Task.FromResult(
            Result<DbConnection>.Fail(SandboxErrors.IsolationPreparationFailed()));

        public Task<Result> GrantRunnerAccessAsync(
            SandboxLease lease,
            SandboxIsolationNamespace sandboxNamespace,
            CancellationToken ct) => Task.FromResult(Result.Success());

        public Task<Result<DbConnection>> OpenRunnerConnectionAsync(
            SandboxLease lease,
            SandboxIsolationNamespace sandboxNamespace,
            CancellationToken ct) => throw new NotSupportedException();

        public Task<Result> CleanupAsync(
            SandboxLease lease,
            SandboxIsolationNamespace sandboxNamespace,
            CancellationToken ct)
        {
            CleanupCalled = true;
            lease.Discard();
            return Task.FromResult(Result.Fail(SandboxErrors.IsolationCleanupFailed()));
        }
    }
}
