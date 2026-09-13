using System.Collections.Concurrent;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using SQLModule.Common.Results;
using SQLModule.Sandbox;
using SQLModule.Sandbox.Pooling;

namespace SQLModule.UnitTests.Sandbox;

public sealed class LocalSandboxLeaseManagerTests
{
    [Fact(DisplayName = "Pool coordinator: один worker не выдаётся двум lease одновременно")]
    public async Task AcquireAsync_LeasesWorkerExclusively()
    {
        var factory = new FakeWorkerFactory();
        var manager = CreateManager(factory);
        var profile = CreateProfile(maxSize: 1);
        var first = (await manager.AcquireAsync(profile, CancellationToken.None)).Value.ShouldNotBeNull();

        var secondTask = manager.AcquireAsync(profile, CancellationToken.None).AsTask();
        await Task.Delay(30);
        secondTask.IsCompleted.ShouldBeFalse();

        await first.DisposeAsync();
        var second = (await secondTask).Value.ShouldNotBeNull();

        second.Worker.ShouldBeSameAs(first.Worker);
        second.Generation.ShouldBeGreaterThan(first.Generation);
        await second.DisposeAsync();
    }

    [Fact(DisplayName = "Pool coordinator: параллельная нагрузка не превышает MaxSize")]
    public async Task AcquireAsync_NeverCreatesMoreThanMaxSize()
    {
        var factory = new FakeWorkerFactory();
        var manager = CreateManager(factory);
        var profile = CreateProfile(maxSize: 2);
        var first = (await manager.AcquireAsync(profile, CancellationToken.None)).Value.ShouldNotBeNull();
        var second = (await manager.AcquireAsync(profile, CancellationToken.None)).Value.ShouldNotBeNull();

        var thirdTask = manager.AcquireAsync(profile, CancellationToken.None).AsTask();
        await Task.Delay(30);

        factory.Created.Count.ShouldBe(2);
        thirdTask.IsCompleted.ShouldBeFalse();
        first.Worker.ShouldNotBeSameAs(second.Worker);

        await first.DisposeAsync();
        var third = (await thirdTask).Value.ShouldNotBeNull();
        factory.Created.Count.ShouldBe(2);

        await second.DisposeAsync();
        await third.DisposeAsync();
    }

    [Fact(DisplayName = "Pool coordinator: timeout не теряет ёмкость очереди")]
    public async Task AcquireAsync_AfterTimeoutCanLeaseReturnedWorker()
    {
        var factory = new FakeWorkerFactory();
        var manager = CreateManager(factory, TimeSpan.FromMilliseconds(50));
        var profile = CreateProfile(maxSize: 1);
        var held = (await manager.AcquireAsync(profile, CancellationToken.None)).Value.ShouldNotBeNull();

        var timedOut = await manager.AcquireAsync(profile, CancellationToken.None);

        timedOut.IsSuccess.ShouldBeFalse();
        timedOut.Error!.Code.ShouldBe("Sandbox.PoolAcquireTimeout");
        await held.DisposeAsync();
        var afterTimeout = await manager.AcquireAsync(profile, CancellationToken.None);
        afterTimeout.IsSuccess.ShouldBeTrue();
        factory.Created.Count.ShouldBe(1);
        await afterTimeout.Value!.DisposeAsync();
    }

    [Fact(DisplayName = "Pool coordinator: cancellation вызывающего запроса пробрасывается")]
    public async Task AcquireAsync_PropagatesCallerCancellation()
    {
        var factory = new FakeWorkerFactory();
        var manager = CreateManager(factory, TimeSpan.FromSeconds(5));
        var profile = CreateProfile(maxSize: 1);
        var held = (await manager.AcquireAsync(profile, CancellationToken.None)).Value.ShouldNotBeNull();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(30));

        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await manager.AcquireAsync(profile, cancellation.Token));

        await held.DisposeAsync();
        var next = (await manager.AcquireAsync(profile, CancellationToken.None)).Value.ShouldNotBeNull();
        await next.DisposeAsync();
    }

    [Fact(DisplayName = "Pool coordinator: профили имеют независимые очереди")]
    public async Task AcquireAsync_DifferentProfilesDoNotBlockEachOther()
    {
        var factory = new FakeWorkerFactory();
        var manager = CreateManager(factory);
        var postgres = CreateProfile("postgres", "postgres:17-alpine", 5432, maxSize: 1);
        var mysql = CreateProfile("mysql", "mysql:8.4", 3306, maxSize: 1);
        var heldPostgres = (await manager.AcquireAsync(postgres, CancellationToken.None)).Value.ShouldNotBeNull();

        var mysqlLease = await manager.AcquireAsync(mysql, CancellationToken.None);

        mysqlLease.IsSuccess.ShouldBeTrue();
        mysqlLease.Value!.Worker.Profile.Key.ShouldBe(mysql.Key);
        factory.Created.Count.ShouldBe(2);
        await heldPostgres.DisposeAsync();
        await mysqlLease.Value.DisposeAsync();
    }

    [Fact(DisplayName = "Pool coordinator: discard удаляет worker и освобождает слот замене")]
    public async Task DisposeAsync_WithDiscardReplacesWorker()
    {
        var factory = new FakeWorkerFactory();
        var manager = CreateManager(factory);
        var profile = CreateProfile(maxSize: 1);
        var discarded = (await manager.AcquireAsync(profile, CancellationToken.None)).Value.ShouldNotBeNull();

        discarded.Discard();
        await discarded.DisposeAsync();
        var replacement = (await manager.AcquireAsync(profile, CancellationToken.None)).Value.ShouldNotBeNull();

        replacement.Worker.WorkerId.ShouldNotBe(discarded.Worker.WorkerId);
        factory.Created.Count.ShouldBe(2);
        factory.Deleted.ShouldContain(discarded.Worker);
        await replacement.DisposeAsync();
    }

    [Fact(DisplayName = "Pool coordinator: ошибка создания будит ожидающий запрос и возвращает слот")]
    public async Task AcquireAsync_CreationFailureWakesWaiter()
    {
        var factory = new FailFirstWorkerFactory();
        var manager = CreateManager(factory, TimeSpan.FromSeconds(2));
        var profile = CreateProfile(maxSize: 1);
        var failedTask = manager.AcquireAsync(profile, CancellationToken.None).AsTask();
        await factory.FirstCreationStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
        var waiterTask = manager.AcquireAsync(profile, CancellationToken.None).AsTask();

        factory.AllowFirstFailure.TrySetResult();
        var failed = await failedTask;
        var waiter = await waiterTask;

        failed.IsSuccess.ShouldBeFalse();
        waiter.IsSuccess.ShouldBeTrue();
        factory.CreateCount.ShouldBe(2);
        await waiter.Value!.DisposeAsync();
    }

    [Fact(DisplayName = "Pool lifecycle: прогревает профиль до MinSize без аренды")]
    public async Task MaintainAsync_PrewarmsProfileToMinSize()
    {
        var factory = new LifecycleWorkerFactory();
        var manager = CreateManager(factory);
        var profile = CreateProfile(minSize: 2, maxSize: 3);

        var maintained = await manager.MaintainAsync([profile], CancellationToken.None);
        var first = (await manager.AcquireAsync(profile, CancellationToken.None)).Value.ShouldNotBeNull();
        var second = (await manager.AcquireAsync(profile, CancellationToken.None)).Value.ShouldNotBeNull();

        maintained.ShouldBeTrue();
        factory.Created.Count.ShouldBe(2);
        first.Worker.ShouldNotBeSameAs(second.Worker);
        await first.DisposeAsync();
        await second.DisposeAsync();
    }

    [Fact(DisplayName = "Pool lifecycle: unhealthy worker удаляется и заменяется до MinSize")]
    public async Task MaintainAsync_ReplacesUnhealthyWorker()
    {
        var factory = new LifecycleWorkerFactory();
        var manager = CreateManager(factory);
        var profile = CreateProfile(minSize: 1, maxSize: 1);
        (await manager.MaintainAsync([profile], CancellationToken.None)).ShouldBeTrue();
        var unhealthy = factory.Created.Single();
        factory.UnhealthyWorkers.TryAdd(unhealthy.WorkerId, 0).ShouldBeTrue();

        var maintained = await manager.MaintainAsync([profile], CancellationToken.None);

        maintained.ShouldBeFalse();
        factory.Deleted.ShouldContain(unhealthy);
        factory.Created.Count.ShouldBe(2);
        var replacement = (await manager.AcquireAsync(profile, CancellationToken.None)).Value.ShouldNotBeNull();
        replacement.Worker.WorkerId.ShouldNotBe(unhealthy.WorkerId);
        await replacement.DisposeAsync();
    }

    [Fact(DisplayName = "Pool lifecycle: повторяет удаление выбракованного worker после сбоя Docker")]
    public async Task MaintainAsync_RetriesFailedQuarantinedWorkerDeletion()
    {
        var factory = new LifecycleWorkerFactory();
        var manager = CreateManager(factory);
        var profile = CreateProfile(minSize: 1, maxSize: 1);
        await manager.MaintainAsync([profile], CancellationToken.None);
        var discarded = (await manager.AcquireAsync(profile, CancellationToken.None)).Value.ShouldNotBeNull();
        factory.FailNextDeletions(1);

        discarded.Discard();
        await discarded.DisposeAsync();
        discarded.Worker.State.ShouldBe(SandboxWorkerState.Unhealthy);

        var maintained = await manager.MaintainAsync([profile], CancellationToken.None);

        maintained.ShouldBeTrue();
        factory.Deleted.Count(worker => worker == discarded.Worker).ShouldBe(2);
        factory.Created.Count.ShouldBe(2);
        var replacement = (await manager.AcquireAsync(profile, CancellationToken.None)).Value.ShouldNotBeNull();
        replacement.Worker.ShouldNotBeSameAs(discarded.Worker);
        await replacement.DisposeAsync();
    }

    [Fact(DisplayName = "Pool lifecycle: drain запрещает новые аренды и удаляет возвращённый worker")]
    public async Task DrainAsync_StopsAcquisitionAndDeletesWorkers()
    {
        var factory = new LifecycleWorkerFactory();
        var manager = CreateManager(factory);
        var profile = CreateProfile(minSize: 1, maxSize: 1);
        await manager.MaintainAsync([profile], CancellationToken.None);
        var lease = (await manager.AcquireAsync(profile, CancellationToken.None)).Value.ShouldNotBeNull();

        var drain = manager.DrainAsync(CancellationToken.None).AsTask();
        await Task.Yield();
        var rejected = await manager.AcquireAsync(profile, CancellationToken.None);
        await lease.DisposeAsync();
        await drain;

        rejected.IsSuccess.ShouldBeFalse();
        rejected.Error!.Code.ShouldBe("Sandbox.PoolIsStopping");
        factory.Deleted.ShouldContain(lease.Worker);
        lease.Worker.State.ShouldBe(SandboxWorkerState.Disposed);
    }

    [Fact(DisplayName = "Pool lifecycle: restart backoff ограничен настроенным максимумом")]
    public void RestartBackoff_IsBounded()
    {
        var first = SandboxPoolHostedService.CalculateRestartDelay(1, 60);
        var repeated = SandboxPoolHostedService.CalculateRestartDelay(20, 60);

        first.ShouldBeInRange(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.2));
        repeated.ShouldBeLessThanOrEqualTo(TimeSpan.FromSeconds(60));
    }

    private static LocalSandboxLeaseManager CreateManager(
        ISandboxWorkerFactory factory,
        TimeSpan? timeout = null) =>
        new(
            factory,
            Options.Create(new SandboxOptions()),
            NullLogger<LocalSandboxLeaseManager>.Instance,
            timeout ?? TimeSpan.FromSeconds(1));

    private static SandboxWorkerProfile CreateProfile(
        string systemName = "postgres",
        string dockerImage = "postgres:17-alpine",
        int port = 5432,
        int minSize = 0,
        int maxSize = 1) =>
        new(
            new SandboxDbmsSpec(
                systemName,
                dockerImage,
                port,
                $"{systemName}_USER",
                "sandbox-user",
                $"{systemName}_PASSWORD",
                "sandbox-password",
                $"{systemName}_DATABASE",
                "training",
                null),
            new SandboxPoolProfileOptions { MinSize = minSize, MaxSize = maxSize });

    private sealed class LifecycleWorkerFactory : ISandboxWorkerFactory
    {
        private int deleteFailuresRemaining;
        internal ConcurrentBag<SandboxWorker> Created { get; } = [];
        internal ConcurrentBag<SandboxWorker> Deleted { get; } = [];
        internal ConcurrentDictionary<Guid, byte> UnhealthyWorkers { get; } = new();

        internal void FailNextDeletions(int count) =>
            Interlocked.Exchange(ref deleteFailuresRemaining, count);

        public ValueTask<Result<SandboxWorker>> CreateAsync(
            SandboxWorkerProfile profile,
            CancellationToken cancellationToken)
        {
            var worker = new SandboxWorker(
                profile,
                $"lifecycle-{Guid.NewGuid():N}",
                "127.0.0.1",
                profile.Dbms.DefaultPort + Created.Count + 1);
            Created.Add(worker);
            return ValueTask.FromResult(Result<SandboxWorker>.Success(worker));
        }

        public ValueTask<Result> DeleteAsync(SandboxWorker worker, CancellationToken cancellationToken)
        {
            Deleted.Add(worker);
            if (Interlocked.Decrement(ref deleteFailuresRemaining) >= 0)
            {
                return ValueTask.FromResult(Result.Fail(
                    SandboxErrors.ContainerFailed("Injected deletion failure.")));
            }

            return ValueTask.FromResult(Result.Success());
        }

        public ValueTask<bool> IsHealthyAsync(
            SandboxWorker worker,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(!UnhealthyWorkers.ContainsKey(worker.WorkerId));
    }

    private sealed class FakeWorkerFactory : ISandboxWorkerFactory
    {
        internal ConcurrentBag<SandboxWorker> Created { get; } = [];
        internal ConcurrentBag<SandboxWorker> Deleted { get; } = [];

        public ValueTask<Result<SandboxWorker>> CreateAsync(
            SandboxWorkerProfile profile,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var worker = new SandboxWorker(
                profile,
                $"fake-{Guid.NewGuid():N}",
                "127.0.0.1",
                profile.Dbms.DefaultPort + Created.Count + 1);
            Created.Add(worker);
            return ValueTask.FromResult(Result<SandboxWorker>.Success(worker));
        }

        public ValueTask<Result> DeleteAsync(SandboxWorker worker, CancellationToken cancellationToken)
        {
            Deleted.Add(worker);
            return ValueTask.FromResult(Result.Success());
        }

        public ValueTask<bool> IsHealthyAsync(
            SandboxWorker worker,
            CancellationToken cancellationToken) => ValueTask.FromResult(true);
    }

    private sealed class FailFirstWorkerFactory : ISandboxWorkerFactory
    {
        private int createCount;

        internal TaskCompletionSource FirstCreationStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource AllowFirstFailure { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal int CreateCount => Volatile.Read(ref createCount);

        public async ValueTask<Result<SandboxWorker>> CreateAsync(
            SandboxWorkerProfile profile,
            CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref createCount) == 1)
            {
                FirstCreationStarted.TrySetResult();
                await AllowFirstFailure.Task.WaitAsync(cancellationToken);
                return Result<SandboxWorker>.Fail(
                    Error.Unavailable("Sandbox.TestCreationFailure", "Expected test failure."));
            }

            return Result<SandboxWorker>.Success(
                new SandboxWorker(profile, "replacement", "127.0.0.1", 54321));
        }

        public ValueTask<Result> DeleteAsync(SandboxWorker worker, CancellationToken cancellationToken) =>
            ValueTask.FromResult(Result.Success());

        public ValueTask<bool> IsHealthyAsync(
            SandboxWorker worker,
            CancellationToken cancellationToken) => ValueTask.FromResult(true);
    }
}
