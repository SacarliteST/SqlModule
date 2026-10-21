using Microsoft.Extensions.Logging;
using SQLModule.Common.Results;

namespace SQLModule.Sandbox.Pooling;

/// <summary>
/// Жизненный цикл воркера: создание и приёмка от фабрики, проверка здоровья, удаление и вывод из эксплуатации.
/// Переходы состояний самого воркера выполняет <see cref="SandboxWorker"/>, учёт слотов — <see cref="ProfilePool"/>.
/// </summary>
internal sealed class SandboxWorkerLifecycle(
    ISandboxWorkerFactory workerFactory,
    SandboxPoolStatePublisher publisher,
    ILogger logger)
{
    internal async ValueTask<Result<SandboxWorker>> CreateAsync(
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
            publisher.Publish(pool, profile.Key.SystemName);
            throw;
        }
        catch (Exception exception)
        {
            pool.ReleaseReservation();
            publisher.Publish(pool, profile.Key.SystemName);
            logger.LogError(
                "Не удалось создать sandbox-воркер профиля {Profile}; тип сбоя {FailureType}",
                profile.Key,
                exception.GetType().Name);
            return Result<SandboxWorker>.Fail(SandboxErrors.PoolWorkerCreationFailed());
        }

        if (!creation.IsSuccess || creation.Value is null)
        {
            pool.ReleaseReservation();
            publisher.Publish(pool, profile.Key.SystemName);
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
            var deletion = await DeleteUntrackedAsync(worker);
            if (deletion.IsSuccess)
            {
                pool.ReleaseReservation();
                publisher.Publish(pool, profile.Key.SystemName);
            }

            logger.LogError(
                "Фабрика вернула некорректный sandbox-воркер {WorkerId} для профиля {Profile} в состоянии {State}",
                worker.WorkerId,
                profile.Key,
                worker.State);
            return Result<SandboxWorker>.Fail(SandboxErrors.PoolWorkerCreationFailed());
        }

        pool.Attach(worker);
        SandboxPoolTelemetry.RecordContainerCreated(profile.Key.SystemName);
        publisher.Publish(pool, profile.Key.SystemName);
        logger.LogInformation(
            "Sandbox-воркер {WorkerId} готов для профиля {Profile}; занято слотов {Allocated}/{MaxSize}",
            worker.WorkerId,
            profile.Key,
            pool.Allocated,
            profile.MaxSize);
        return Result<SandboxWorker>.Success(worker);
    }

    /// <summary>Проверяет здоровье воркера; сбой самой проверки считается нездоровым воркером.</summary>
    internal async ValueTask<bool> IsHealthyAsync(SandboxWorker worker, CancellationToken cancellationToken)
    {
        try
        {
            return await workerFactory.IsHealthyAsync(worker, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                "Проверка здоровья sandbox-воркера {WorkerId} профиля {Profile} завершилась сбоем типа {FailureType}",
                worker.WorkerId,
                worker.Profile.Key,
                exception.GetType().Name);
            return false;
        }
    }

    internal async ValueTask RetireUnexpectedAsync(ProfilePool pool, SandboxWorker worker)
    {
        worker.TryMarkUnhealthy();
        logger.LogWarning(
            "Sandbox-воркер {WorkerId} профиля {Profile} удаляется из-за неожиданного состояния {State}",
            worker.WorkerId,
            worker.Profile.Key,
            worker.State);
        await DeleteTrackedAsync(pool, worker, null);
    }

    internal async ValueTask<bool> DeleteTrackedAsync(
        ProfilePool pool,
        SandboxWorker worker,
        Guid? leaseId,
        CancellationToken cancellationToken = default)
    {
        var deletion = await DeleteSafelyAsync(worker, cancellationToken);
        if (deletion.IsSuccess)
        {
            worker.TryMarkDisposed();
            pool.Remove(worker);
            SandboxPoolTelemetry.RecordContainerDeleted(worker.Profile.Key.SystemName);
            publisher.Publish(pool, worker.Profile.Key.SystemName);
            logger.LogInformation(
                "Sandbox-воркер {WorkerId} профиля {Profile} удалён после аренды {LeaseId}",
                worker.WorkerId,
                worker.Profile.Key,
                leaseId);
        }

        return deletion.IsSuccess;
    }

    private async ValueTask<Result> DeleteUntrackedAsync(SandboxWorker worker)
    {
        var deletion = await DeleteSafelyAsync(worker);
        if (deletion.IsSuccess)
        {
            worker.TryMarkDisposed();
        }

        return deletion;
    }

    private async ValueTask<Result> DeleteSafelyAsync(
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
}
