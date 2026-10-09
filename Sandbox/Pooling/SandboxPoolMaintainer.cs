using Microsoft.Extensions.Logging;

namespace SQLModule.Sandbox.Pooling;

/// <summary>
/// Фоновое обслуживание пула профиля: повтор удаления выбракованных воркеров, проверка готовых воркеров
/// и восстановление MinSize.
/// </summary>
internal sealed class SandboxPoolMaintainer(
    SandboxWorkerLifecycle lifecycle,
    SandboxPoolStatePublisher publisher,
    ILogger logger)
{
    /// <summary>Возвращает <see langword="false"/>, если часть работы не удалась (пул будет обслужен повторно).</summary>
    internal async ValueTask<bool> MaintainAsync(
        ProfilePool pool,
        SandboxWorkerProfile profile,
        CancellationToken cancellationToken)
    {
        var succeeded = true;
        succeeded &= await RetryUnhealthyWorkerDeletionAsync(pool, cancellationToken);
        succeeded &= await CheckReadyWorkersAsync(pool, cancellationToken);
        while (pool.Allocated < pool.MinSize && pool.TryReserveSlot())
        {
            publisher.Publish(pool, profile.Key.SystemName);
            var creation = await lifecycle.CreateAsync(pool, profile, cancellationToken);
            if (!creation.IsSuccess)
            {
                succeeded = false;
                break;
            }

            pool.ReturnReady(creation.Value!);
        }

        publisher.Publish(pool, profile.Key.SystemName);
        return succeeded;
    }

    private async ValueTask<bool> CheckReadyWorkersAsync(ProfilePool pool, CancellationToken cancellationToken)
    {
        var succeeded = true;
        foreach (var worker in pool.TakeReadySnapshot())
        {
            bool healthy;
            try
            {
                healthy = await lifecycle.IsHealthyAsync(worker, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                pool.ReturnReady(worker);
                throw;
            }

            if (healthy)
            {
                pool.ReturnReady(worker);
                continue;
            }

            succeeded = false;
            worker.TryMarkUnhealthy();
            SandboxPoolTelemetry.RecordReplacement(worker.Profile.Key.SystemName, "health");
            publisher.Publish(pool, worker.Profile.Key.SystemName);
            logger.LogWarning(
                "Sandbox-воркер {WorkerId} профиля {Profile} не прошёл проверку здоровья и будет заменён",
                worker.WorkerId,
                worker.Profile.Key);
            await lifecycle.DeleteTrackedAsync(pool, worker, null, cancellationToken);
        }

        return succeeded;
    }

    private async ValueTask<bool> RetryUnhealthyWorkerDeletionAsync(
        ProfilePool pool,
        CancellationToken cancellationToken)
    {
        var succeeded = true;
        foreach (var worker in pool.SnapshotWorkers()
                     .Where(worker => worker.State == SandboxWorkerState.Unhealthy))
        {
            logger.LogWarning(
                "Повторяется удаление выбракованного sandbox-воркера {WorkerId} профиля {Profile}",
                worker.WorkerId,
                worker.Profile.Key);
            succeeded &= await lifecycle.DeleteTrackedAsync(pool, worker, null, cancellationToken);
        }

        return succeeded;
    }
}
