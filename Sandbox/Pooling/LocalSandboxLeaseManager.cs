using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SQLModule.Common.Results;

namespace SQLModule.Sandbox.Pooling;

/// <summary>
/// Локальный координатор эксклюзивных аренд. Каждый профиль имеет собственную очередь и лимит.
/// Контейнерный lifecycle подключается отдельно через <see cref="ISandboxWorkerFactory"/>.
/// </summary>
internal sealed class LocalSandboxLeaseManager : ISandboxLeaseManager, ISandboxPoolLifecycle
{
    private readonly ConcurrentDictionary<SandboxProfileKey, ProfilePool> pools = new();
    private readonly ISandboxWorkerFactory workerFactory;
    private readonly ILogger<LocalSandboxLeaseManager> logger;
    private readonly TimeSpan acquireTimeout;
    private readonly CancellationTokenSource draining = new();
    private int isDraining;

    internal LocalSandboxLeaseManager(
        ISandboxWorkerFactory workerFactory,
        IOptions<SandboxOptions> options,
        ILogger<LocalSandboxLeaseManager> logger,
        TimeSpan? acquireTimeout = null)
    {
        this.workerFactory = workerFactory;
        this.logger = logger;
        this.acquireTimeout = acquireTimeout ??
            TimeSpan.FromSeconds(options.Value.Pool.AcquireTimeoutSeconds);
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

        var pool = pools.GetOrAdd(profile.Key, _ => new ProfilePool(profile));
        if (!pool.HasLimits(profile))
        {
            logger.LogError(
                "Конфликт конфигурации профиля пула {Profile}; переданы границы {MinSize}..{MaxSize}",
                profile.Key,
                profile.MinSize,
                profile.MaxSize);
            return Result<SandboxLease>.Fail(SandboxErrors.PoolProfileConflict());
        }

        var startedAt = Stopwatch.GetTimestamp();
        using var timeoutSource = new CancellationTokenSource(acquireTimeout);
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

                    await RetireUnexpectedWorkerAsync(pool, readyWorker);
                    continue;
                }

                if (pool.TryReserveSlot())
                {
                    var creation = await CreateWorkerAsync(pool, profile, linkedSource.Token);
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

                    await RetireUnexpectedWorkerAsync(pool, worker);
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
            var pool = pools.GetOrAdd(profile.Key, _ => new ProfilePool(profile));
            if (!pool.HasLimits(profile))
            {
                logger.LogError(
                    "Конфликт конфигурации профиля пула {Profile}; переданы границы {MinSize}..{MaxSize}",
                    profile.Key,
                    profile.MinSize,
                    profile.MaxSize);
                succeeded = false;
                continue;
            }

            succeeded &= await CheckReadyWorkersAsync(pool, cancellationToken);
            while (pool.Allocated < pool.MinSize && pool.TryReserveSlot())
            {
                var creation = await CreateWorkerAsync(pool, profile, cancellationToken);
                if (!creation.IsSuccess)
                {
                    succeeded = false;
                    break;
                }

                pool.ReturnReady(creation.Value!);
            }
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
            while (pools.Values.Any(pool => pool.HasActiveLease))
            {
                await Task.Delay(TimeSpan.FromMilliseconds(25), cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                "Истекло время ожидания активных аренд sandbox; оставшиеся воркеры будут удалены принудительно");
        }

        foreach (var pool in pools.Values)
        {
            foreach (var worker in pool.SnapshotWorkers())
            {
                worker.TryMarkUnhealthy();
                await DeleteTrackedWorkerAsync(pool, worker, null, CancellationToken.None);
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
        return Result<SandboxLease>.Success(lease);
    }

    private async ValueTask<Result<SandboxWorker>> CreateWorkerAsync(
        ProfilePool pool,
        SandboxWorkerProfile profile,
        CancellationToken cancellationToken)
    {
        Result<SandboxWorker> creation;
        try
        {
            logger.LogInformation("Создаётся sandbox-воркер для профиля {Profile}", profile.Key);
            creation = await workerFactory.CreateAsync(profile, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            pool.ReleaseReservation();
            throw;
        }
        catch (Exception exception)
        {
            pool.ReleaseReservation();
            logger.LogError(
                "Не удалось создать sandbox-воркер профиля {Profile}; тип сбоя {FailureType}",
                profile.Key,
                exception.GetType().Name);
            return Result<SandboxWorker>.Fail(SandboxErrors.PoolWorkerCreationFailed());
        }

        if (!creation.IsSuccess || creation.Value is null)
        {
            pool.ReleaseReservation();
            logger.LogWarning(
                "Создание sandbox-воркера профиля {Profile} отклонено с кодом {ErrorCode}",
                profile.Key,
                creation.Error?.Code ?? "Sandbox.EmptyWorker");
            return creation.IsSuccess
                ? Result<SandboxWorker>.Fail(SandboxErrors.PoolWorkerCreationFailed())
                : creation;
        }

        var worker = creation.Value;
        if (worker.Profile.Key != profile.Key ||
            (worker.State == SandboxWorkerState.Starting && !worker.TryMarkReady()) ||
            worker.State != SandboxWorkerState.Ready)
        {
            worker.TryMarkUnhealthy();
            var deletion = await DeleteUntrackedWorkerAsync(worker);
            if (deletion.IsSuccess)
            {
                pool.ReleaseReservation();
            }

            logger.LogError(
                "Фабрика вернула некорректный sandbox-воркер {WorkerId} для профиля {Profile} в состоянии {State}",
                worker.WorkerId,
                profile.Key,
                worker.State);
            return Result<SandboxWorker>.Fail(SandboxErrors.PoolWorkerCreationFailed());
        }

        pool.Attach(worker);
        logger.LogInformation(
            "Sandbox-воркер {WorkerId} готов для профиля {Profile}; занято слотов {Allocated}/{MaxSize}",
            worker.WorkerId,
            profile.Key,
            pool.Allocated,
            profile.MaxSize);
        return Result<SandboxWorker>.Success(worker);
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

        await DeleteTrackedWorkerAsync(pool, worker, lease.LeaseId);
    }

    private async ValueTask<bool> CheckReadyWorkersAsync(
        ProfilePool pool,
        CancellationToken cancellationToken)
    {
        var succeeded = true;
        foreach (var worker in pool.TakeReadySnapshot())
        {
            bool healthy;
            try
            {
                healthy = await workerFactory.IsHealthyAsync(worker, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                pool.ReturnReady(worker);
                throw;
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    "Проверка здоровья sandbox-воркера {WorkerId} профиля {Profile} завершилась сбоем типа {FailureType}",
                    worker.WorkerId,
                    worker.Profile.Key,
                    exception.GetType().Name);
                healthy = false;
            }

            if (healthy)
            {
                pool.ReturnReady(worker);
                continue;
            }

            succeeded = false;
            worker.TryMarkUnhealthy();
            logger.LogWarning(
                "Sandbox-воркер {WorkerId} профиля {Profile} не прошёл проверку здоровья и будет заменён",
                worker.WorkerId,
                worker.Profile.Key);
            await DeleteTrackedWorkerAsync(pool, worker, null, cancellationToken);
        }

        return succeeded;
    }

    private async ValueTask RetireUnexpectedWorkerAsync(ProfilePool pool, SandboxWorker worker)
    {
        worker.TryMarkUnhealthy();
        logger.LogWarning(
            "Sandbox-воркер {WorkerId} профиля {Profile} удаляется из-за неожиданного состояния {State}",
            worker.WorkerId,
            worker.Profile.Key,
            worker.State);
        await DeleteTrackedWorkerAsync(pool, worker, null);
    }

    private async ValueTask DeleteTrackedWorkerAsync(
        ProfilePool pool,
        SandboxWorker worker,
        Guid? leaseId,
        CancellationToken cancellationToken = default)
    {
        var deletion = await DeleteWorkerSafelyAsync(worker, cancellationToken);
        if (deletion.IsSuccess)
        {
            worker.TryMarkDisposed();
            pool.Remove(worker);
            logger.LogInformation(
                "Sandbox-воркер {WorkerId} профиля {Profile} удалён после аренды {LeaseId}",
                worker.WorkerId,
                worker.Profile.Key,
                leaseId);
        }
    }

    private async ValueTask<Result> DeleteUntrackedWorkerAsync(SandboxWorker worker)
    {
        var deletion = await DeleteWorkerSafelyAsync(worker);
        if (deletion.IsSuccess)
        {
            worker.TryMarkDisposed();
        }

        return deletion;
    }

    private async ValueTask<Result> DeleteWorkerSafelyAsync(
        SandboxWorker worker,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var deletion = await workerFactory.DeleteAsync(worker, cancellationToken);
            if (!deletion.IsSuccess)
            {
                logger.LogError(
                    "Не удалось удалить sandbox-воркер {WorkerId} профиля {Profile}; код {ErrorCode}",
                    worker.WorkerId,
                    worker.Profile.Key,
                    deletion.Error?.Code ?? "Sandbox.UnknownDeletionFailure");
            }

            return deletion;
        }
        catch (Exception exception)
        {
            logger.LogError(
                "Не удалось удалить sandbox-воркер {WorkerId} профиля {Profile}; тип сбоя {FailureType}",
                worker.WorkerId,
                worker.Profile.Key,
                exception.GetType().Name);
            return Result.Fail(SandboxErrors.ContainerFailed("Sandbox worker deletion failed."));
        }
    }

    private sealed class ProfilePool
    {
        private readonly object sync = new();
        private readonly ConcurrentQueue<SandboxWorker> ready = new();
        private readonly SemaphoreSlim changed = new(0);
        private readonly Dictionary<Guid, SandboxWorker> workers = [];
        private int allocated;

        internal ProfilePool(SandboxWorkerProfile profile)
        {
            MinSize = profile.MinSize;
            MaxSize = profile.MaxSize;
        }

        internal int MinSize { get; }
        internal int MaxSize { get; }

        internal int Allocated
        {
            get
            {
                lock (sync)
                {
                    return allocated;
                }
            }
        }

        internal bool HasLimits(SandboxWorkerProfile profile) =>
            profile.MinSize == MinSize && profile.MaxSize == MaxSize;

        internal bool TryReserveSlot()
        {
            lock (sync)
            {
                if (allocated >= MaxSize)
                {
                    return false;
                }

                allocated++;
                return true;
            }
        }

        internal void Attach(SandboxWorker worker)
        {
            lock (sync)
            {
                if (!workers.TryAdd(worker.WorkerId, worker))
                {
                    throw new InvalidOperationException("Worker уже зарегистрирован в пуле.");
                }
            }
        }

        internal void ReleaseReservation()
        {
            lock (sync)
            {
                allocated--;
            }

            changed.Release();
        }

        internal void Remove(SandboxWorker worker)
        {
            lock (sync)
            {
                if (workers.Remove(worker.WorkerId))
                {
                    allocated--;
                }
            }

            changed.Release();
        }

        internal bool TryTakeReady(out SandboxWorker worker)
        {
            if (!ready.TryDequeue(out worker!))
            {
                return false;
            }

            changed.Wait(0);
            return true;
        }

        internal void ReturnReady(SandboxWorker worker)
        {
            ready.Enqueue(worker);
            changed.Release();
        }

        internal Task WaitForChangeAsync(CancellationToken cancellationToken) =>
            changed.WaitAsync(cancellationToken);

        internal bool HasActiveLease => SnapshotWorkers().Any(worker =>
            worker.State is SandboxWorkerState.Leased or SandboxWorkerState.Recycling);

        internal IReadOnlyCollection<SandboxWorker> SnapshotWorkers()
        {
            lock (sync)
            {
                return workers.Values.ToArray();
            }
        }

        internal IReadOnlyCollection<SandboxWorker> TakeReadySnapshot()
        {
            var result = new List<SandboxWorker>();
            while (ready.TryDequeue(out var worker))
            {
                changed.Wait(0);
                result.Add(worker);
            }

            return result;
        }
    }
}
