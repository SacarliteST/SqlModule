using System.Collections.Concurrent;

namespace SQLModule.Sandbox.Pooling;

/// <summary>
/// Состояние пула одного профиля: очередь готовых воркеров, счётчик занятых слотов и сигнал об изменениях.
/// Сам не создаёт и не удаляет контейнеры — этим занимается <see cref="SandboxWorkerLifecycle"/>.
/// </summary>
internal sealed class ProfilePool
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

    internal int ActiveLeaseCount => SnapshotWorkers().Count(worker =>
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
