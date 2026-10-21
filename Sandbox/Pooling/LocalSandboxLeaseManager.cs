using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SQLModule.Common.Results;

namespace SQLModule.Sandbox.Pooling;

/// <summary>
/// Локальный координатор эксклюзивных аренд: выдаёт и принимает lease, ожидает свободный слот и останавливает пул.
/// Хранение пулов — <see cref="SandboxPoolRegistry"/>, жизненный цикл контейнеров —
/// <see cref="SandboxWorkerLifecycle"/> (поверх <see cref="ISandboxWorkerFactory"/>),
/// фоновое обслуживание — <see cref="SandboxPoolMaintainer"/>.
/// </summary>
internal sealed class LocalSandboxLeaseManager : ISandboxLeaseManager, ISandboxPoolLifecycle
{
    private readonly SandboxPoolRegistry registry;
    private readonly SandboxWorkerLifecycle lifecycle;
    private readonly SandboxPoolMaintainer maintainer;
    private readonly SandboxPoolStatePublisher publisher;
    private readonly ILogger<LocalSandboxLeaseManager> logger;
    private readonly TimeSpan acquireTimeout;
    private readonly TimeProvider timeProvider;
    private readonly CancellationTokenSource draining = new();
    private int isDraining;

    internal LocalSandboxLeaseManager(
        ISandboxWorkerFactory workerFactory,
        IOptions<SandboxOptions> options,
        ILogger<LocalSandboxLeaseManager> logger,
        TimeSpan? acquireTimeout = null,
        SandboxPoolHealthMonitor? healthMonitor = null,
        TimeProvider? timeProvider = null)
    {
        this.logger = logger;
        publisher = new SandboxPoolStatePublisher(healthMonitor ?? new SandboxPoolHealthMonitor(options));
        registry = new SandboxPoolRegistry(logger);
        lifecycle = new SandboxWorkerLifecycle(workerFactory, publisher, logger);
        maintainer = new SandboxPoolMaintainer(lifecycle, publisher, logger);
        this.acquireTimeout = acquireTimeout ??
            TimeSpan.FromSeconds(options.Value.Pool.AcquireTimeoutSeconds);
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async ValueTask<Result<SandboxLease>> AcquireAsync(
        SandboxWorkerProfile profile,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (Volatile.Read(ref isDraining) != 0)
        {
            return Result<SandboxLease>.Fail(SandboxErrors.PoolIsStopping());
        }

        if (!registry.TryGetPool(profile, out var pool))
        {
            return Result<SandboxLease>.Fail(SandboxErrors.PoolProfileConflict());
        }

        var startedAt = Stopwatch.GetTimestamp();
        using var timeoutSource = new CancellationTokenSource(acquireTimeout, timeProvider);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            draining.Token,
            timeoutSource.Token);

        try
        {
            while (true)
            {
                linkedSource.Token.ThrowIfCancellationRequested();

                if (pool.TryTakeReady(out var readyWorker))
                {
                    if (readyWorker.TryLease(out var generation))
                    {
                        return CreateLease(pool, readyWorker, generation, startedAt);
                    }

                    await lifecycle.RetireUnexpectedAsync(pool, readyWorker);
                    continue;
                }

                if (pool.TryReserveSlot())
                {
                    publisher.Publish(pool, profile.Key.SystemName);
                    var creation = await lifecycle.CreateAsync(pool, profile, linkedSource.Token);
                    if (!creation.IsSuccess)
                    {
                        return Result<SandboxLease>.Fail(creation.Error!);
                    }

                    var worker = creation.Value!;
                    if (linkedSource.IsCancellationRequested)
                    {
                        pool.ReturnReady(worker);
                        linkedSource.Token.ThrowIfCancellationRequested();
                    }

                    if (worker.TryLease(out var generation))
                    {
                        return CreateLease(pool, worker, generation, startedAt);
                    }

                    await lifecycle.RetireUnexpectedAsync(pool, worker);
                    continue;
                }

                await pool.WaitForChangeAsync(linkedSource.Token);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            if (draining.IsCancellationRequested)
            {
                return Result<SandboxLease>.Fail(SandboxErrors.PoolIsStopping());
            }

            logger.LogWarning(
                "Истекло время ожидания аренды sandbox для профиля {Profile}; ожидание {ElapsedMs} мс",
                profile.Key,
                Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
            SandboxPoolTelemetry.RecordAcquireTimeout(profile.Key.SystemName);
            SandboxPoolTelemetry.RecordLeaseWait(
                profile.Key.SystemName,
                Stopwatch.GetElapsedTime(startedAt),
                "timeout");
            return Result<SandboxLease>.Fail(SandboxErrors.PoolAcquireTimeout());
        }
    }

    public async ValueTask<bool> MaintainAsync(
        IReadOnlyCollection<SandboxWorkerProfile> profiles,
        CancellationToken cancellationToken)
    {
        if (Volatile.Read(ref isDraining) != 0)
        {
            return false;
        }

        var succeeded = true;
        foreach (var profile in profiles)
        {
            if (!registry.TryGetPool(profile, out var pool))
            {
                succeeded = false;
                continue;
            }

            succeeded &= await maintainer.MaintainAsync(pool, profile, cancellationToken);
        }

        return succeeded;
    }

    public async ValueTask DrainAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref isDraining, 1) == 0)
        {
            logger.LogInformation("Остановлена выдача новых аренд sandbox; начинается опустошение пула");
            await draining.CancelAsync();
        }

        try
        {
            while (registry.Pools.Any(pool => pool.HasActiveLease))
            {
                await Task.Delay(TimeSpan.FromMilliseconds(25), timeProvider, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            var activeLeases = registry.Pools.Sum(pool => pool.ActiveLeaseCount);
            SandboxPoolTelemetry.RecordForcedShutdown(activeLeases);
            logger.LogWarning(
                "Истекло время ожидания активных аренд sandbox; {ActiveLeases} воркеров будут удалены принудительно",
                activeLeases);
        }

        foreach (var pool in registry.Pools)
        {
            foreach (var worker in pool.SnapshotWorkers())
            {
                worker.TryMarkUnhealthy();
                await lifecycle.DeleteTrackedAsync(pool, worker, null, CancellationToken.None);
            }
        }

        logger.LogInformation("Опустошение локального пула sandbox завершено");
    }

    private Result<SandboxLease> CreateLease(
        ProfilePool pool,
        SandboxWorker worker,
        long generation,
        long startedAt)
    {
        var lease = new SandboxLease(
            worker,
            generation,
            releasedLease => ReleaseAsync(pool, releasedLease));
        logger.LogInformation(
            "Аренда sandbox {LeaseId} получила воркер {WorkerId} профиля {Profile}; поколение {Generation}, ожидание {ElapsedMs} мс",
            lease.LeaseId,
            worker.WorkerId,
            worker.Profile.Key,
            generation,
            Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
        SandboxPoolTelemetry.RecordLeaseWait(
            worker.Profile.Key.SystemName,
            Stopwatch.GetElapsedTime(startedAt),
            "success");
        publisher.Publish(pool, worker.Profile.Key.SystemName);
        return Result<SandboxLease>.Success(lease);
    }

    private async ValueTask ReleaseAsync(ProfilePool pool, SandboxLease lease)
    {
        var worker = lease.Worker;
        if (lease.Disposition == SandboxLeaseDisposition.Recycle &&
            Volatile.Read(ref isDraining) == 0)
        {
            if (!worker.TryBeginRecycling(lease.Generation))
            {
                logger.LogWarning(
                    "Устаревшая аренда sandbox {LeaseId} проигнорирована для воркера {WorkerId} профиля {Profile}; поколение {Generation}",
                    lease.LeaseId,
                    worker.WorkerId,
                    worker.Profile.Key,
                    lease.Generation);
                return;
            }

            if (worker.TryCompleteRecycling(lease.Generation))
            {
                pool.ReturnReady(worker);
                publisher.Publish(pool, worker.Profile.Key.SystemName);
                logger.LogInformation(
                    "Аренда sandbox {LeaseId} вернула воркер {WorkerId} в профиль {Profile}; поколение {Generation}",
                    lease.LeaseId,
                    worker.WorkerId,
                    worker.Profile.Key,
                    lease.Generation);
                return;
            }
        }
        else if (!worker.TryMarkUnhealthy(lease.Generation))
        {
            logger.LogWarning(
                "Устаревшая выбракованная аренда sandbox {LeaseId} проигнорирована для воркера {WorkerId} профиля {Profile}; поколение {Generation}",
                lease.LeaseId,
                worker.WorkerId,
                worker.Profile.Key,
                lease.Generation);
            return;
        }

        SandboxPoolTelemetry.RecordReplacement(worker.Profile.Key.SystemName, "discard");
        publisher.Publish(pool, worker.Profile.Key.SystemName);

        await lifecycle.DeleteTrackedAsync(pool, worker, lease.LeaseId);
    }
}
