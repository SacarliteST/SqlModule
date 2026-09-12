using Shouldly;
using SQLModule.Common.Results;
using SQLModule.Sandbox;
using SQLModule.Sandbox.Pooling;

namespace SQLModule.UnitTests.Sandbox;

public sealed class SandboxPoolContractsTests
{
    [Fact(DisplayName = "Pool profile: ключ нормализуется и не зависит от порядка environment")]
    public void ProfileKey_IsStableForEquivalentConfiguration()
    {
        var first = CreateProfile(extraEnv: "TZ=UTC;POSTGRES_INITDB_ARGS=--encoding=UTF8");
        var second = CreateProfile(
            systemName: " PoStGrEs ",
            dockerImage: " postgres:17-alpine ",
            extraEnv: "postgres_initdb_args=--encoding=UTF8;tz=UTC");

        first.Key.ShouldBe(second.Key);
        first.Key.SystemName.ShouldBe("postgres");
        first.Key.DockerImage.ShouldBe("postgres:17-alpine");
    }

    [Fact(DisplayName = "Pool profile: изменение environment создаёт другой fingerprint")]
    public void ProfileKey_ChangesWhenEnvironmentChanges()
    {
        var first = CreateProfile(password: "first-password");
        var second = CreateProfile(password: "second-password");

        first.Key.EnvironmentFingerprint.ShouldNotBe(second.Key.EnvironmentFingerprint);
    }

    [Fact(DisplayName = "Pool contracts: строковые представления не раскрывают секреты запуска")]
    public void DiagnosticIdentity_DoesNotExposeSecrets()
    {
        const string password = "top-secret-password";
        const string username = "private-user";
        const string extraSecret = "extra-secret-value";
        var profile = CreateProfile(
            username: username,
            password: password,
            extraEnv: $"API_TOKEN={extraSecret}");
        var worker = CreateReadyWorker(profile);
        worker.TryLease(out var generation).ShouldBeTrue();
        var lease = new SandboxLease(worker, generation, _ => ValueTask.CompletedTask);

        var diagnostics = String.Join('|', profile.Key, profile, worker, lease);

        diagnostics.ShouldNotContain(password);
        diagnostics.ShouldNotContain(username);
        diagnostics.ShouldNotContain(extraSecret);
        diagnostics.ShouldNotContain("API_TOKEN");
    }

    [Fact(DisplayName = "Worker: поколения защищают от освобождения устаревшего lease")]
    public void WorkerGeneration_RejectsStaleLeaseTransitions()
    {
        var worker = CreateReadyWorker(CreateProfile());

        worker.TryLease(out var firstGeneration).ShouldBeTrue();
        worker.TryBeginRecycling(firstGeneration + 1).ShouldBeFalse();
        worker.State.ShouldBe(SandboxWorkerState.Leased);
        worker.TryBeginRecycling(firstGeneration).ShouldBeTrue();
        worker.TryCompleteRecycling(firstGeneration).ShouldBeTrue();
        worker.TryLease(out var secondGeneration).ShouldBeTrue();

        secondGeneration.ShouldBeGreaterThan(firstGeneration);
        worker.TryBeginRecycling(firstGeneration).ShouldBeFalse();
        worker.State.ShouldBe(SandboxWorkerState.Leased);
    }

    [Fact(DisplayName = "Worker: соблюдает модель состояний и терминальный Disposed")]
    public void WorkerStateMachine_UsesExpectedTransitions()
    {
        var worker = new SandboxWorker(CreateProfile(), "container-1", "127.0.0.1", 54321);

        worker.State.ShouldBe(SandboxWorkerState.Starting);
        worker.TryLease(out _).ShouldBeFalse();
        worker.TryMarkReady().ShouldBeTrue();
        worker.TryMarkReady().ShouldBeFalse();
        worker.TryMarkUnhealthy().ShouldBeTrue();
        worker.TryMarkDisposed().ShouldBeTrue();
        worker.TryMarkReady().ShouldBeFalse();
        worker.TryMarkUnhealthy().ShouldBeFalse();
        worker.TryMarkDisposed().ShouldBeFalse();
    }

    [Fact(DisplayName = "Lease: повторный конкурентный DisposeAsync освобождает worker один раз")]
    public async Task Lease_DisposeAsyncIsIdempotent()
    {
        var worker = CreateReadyWorker(CreateProfile());
        worker.TryLease(out var generation).ShouldBeTrue();
        var releaseCount = 0;
        var lease = new SandboxLease(
            worker,
            generation,
            _ =>
            {
                Interlocked.Increment(ref releaseCount);
                return ValueTask.CompletedTask;
            });

        await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => lease.DisposeAsync().AsTask()));

        releaseCount.ShouldBe(1);
        lease.Disposition.ShouldBe(SandboxLeaseDisposition.Recycle);
    }

    [Fact(DisplayName = "Lease: повреждённый worker помечается для удаления")]
    public async Task Lease_DiscardIsPassedToReleaseCallback()
    {
        var worker = CreateReadyWorker(CreateProfile());
        worker.TryLease(out var generation).ShouldBeTrue();
        SandboxLeaseDisposition? releasedAs = null;
        var lease = new SandboxLease(
            worker,
            generation,
            releasedLease =>
            {
                releasedAs = releasedLease.Disposition;
                return ValueTask.CompletedTask;
            });

        lease.Discard();
        await lease.DisposeAsync();

        releasedAs.ShouldBe(SandboxLeaseDisposition.Discard);
    }

    [Fact(DisplayName = "Worker factory: контракт можно реализовать и тестировать без Docker")]
    public async Task WorkerFactory_CanBeReplacedWithDockerFreeFake()
    {
        var factory = new FakeWorkerFactory();
        var profile = CreateProfile();

        var created = await factory.CreateAsync(profile, CancellationToken.None);
        var worker = created.Value.ShouldNotBeNull();
        var deleted = await factory.DeleteAsync(worker, CancellationToken.None);

        created.IsSuccess.ShouldBeTrue();
        deleted.IsSuccess.ShouldBeTrue();
        factory.CreatedProfiles.ShouldBe([profile]);
        factory.DeletedWorkers.ShouldBe([worker]);
    }

    private static SandboxWorkerProfile CreateProfile(
        string systemName = "postgres",
        string dockerImage = "postgres:17-alpine",
        string username = "sandbox",
        string password = "sandbox-password",
        string? extraEnv = null) =>
        new(
            new SandboxDbmsSpec(
                systemName,
                dockerImage,
                5432,
                "POSTGRES_USER",
                username,
                "POSTGRES_PASSWORD",
                password,
                "POSTGRES_DB",
                "training",
                extraEnv),
            new SandboxPoolProfileOptions { MinSize = 1, MaxSize = 2 });

    private static SandboxWorker CreateReadyWorker(SandboxWorkerProfile profile)
    {
        var worker = new SandboxWorker(profile, "container-1", "127.0.0.1", 54321);
        worker.TryMarkReady().ShouldBeTrue();
        return worker;
    }

    private sealed class FakeWorkerFactory : ISandboxWorkerFactory
    {
        internal List<SandboxWorkerProfile> CreatedProfiles { get; } = [];
        internal List<SandboxWorker> DeletedWorkers { get; } = [];

        public ValueTask<Result<SandboxWorker>> CreateAsync(
            SandboxWorkerProfile profile,
            CancellationToken cancellationToken)
        {
            CreatedProfiles.Add(profile);
            var worker = new SandboxWorker(profile, "fake-container", "127.0.0.1", 54321);
            return ValueTask.FromResult(Result<SandboxWorker>.Success(worker));
        }

        public ValueTask<Result> DeleteAsync(SandboxWorker worker, CancellationToken cancellationToken)
        {
            DeletedWorkers.Add(worker);
            worker.TryMarkDisposed();
            return ValueTask.FromResult(Result.Success());
        }
    }
}
