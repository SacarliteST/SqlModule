using System.Collections.Concurrent;
using System.Diagnostics;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SQLModule.Common.Results;
using SQLModule.Sandbox.Dialects;

namespace SQLModule.Sandbox.Pooling;

internal sealed class TestcontainersSandboxWorkerFactory(
    ISqlDialectFactory dialectFactory,
    IOptions<SandboxOptions> options,
    SandboxPoolInstance instance,
    ILogger<TestcontainersSandboxWorkerFactory> logger) : ISandboxWorkerFactory
{
    private const string OwnerLabel = "sqltren.sqlmodule.sandbox-pool";
    private const string InstanceLabel = "sqltren.sqlmodule.sandbox-pool.instance";
    private const string ProfileLabel = "sqltren.sqlmodule.sandbox-pool.profile";
    private readonly ConcurrentDictionary<Guid, IContainer> containers = new();

    public async ValueTask<Result<SandboxWorker>> CreateAsync(
        SandboxWorkerProfile profile,
        CancellationToken cancellationToken)
    {
        var startedAt = Stopwatch.GetTimestamp();
        var dbms = profile.Dbms;
        if (!SandboxContainerSecurity.IsImagePinned(dbms.DockerImage))
        {
            logger.LogError(
                "Профиль {Profile} отклонён: образ тёплого sandbox-контейнера должен иметь точный tag или digest",
                profile.Key);
            return Result<SandboxWorker>.Fail(SandboxErrors.UnpinnedPoolImage());
        }

        var container = new ContainerBuilder()
            .WithImage(dbms.DockerImage)
            .WithPortBinding(dbms.DefaultPort, true)
            .WithEnvironment(SandboxContainerEnvironment.Build(dbms))
            .WithLabel(OwnerLabel, Boolean.TrueString.ToLowerInvariant())
            .WithLabel(InstanceLabel, instance.Id)
            .WithLabel(ProfileLabel, profile.Key.ToString())
            .WithPrivileged(false)
            .WithCreateParameterModifier(parameters =>
                SandboxContainerSecurity.Apply(parameters, options.Value.Pool.Resources))
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(dbms.DefaultPort))
            .Build();

        using var startup = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        startup.CancelAfter(TimeSpan.FromSeconds(options.Value.ContainerStartupTimeoutSeconds));
        try
        {
            await container.StartAsync(startup.Token);
            var worker = new SandboxWorker(
                profile,
                container.Id,
                container.Hostname,
                container.GetMappedPublicPort(dbms.DefaultPort));
            if (!await CanConnectAsync(worker, startup.Token, retry: true))
            {
                await DisposeAfterFailedStartAsync(container, profile);
                return Result<SandboxWorker>.Fail(SandboxErrors.PoolWorkerCreationFailed());
            }

            if (!containers.TryAdd(worker.WorkerId, container))
            {
                await DisposeAfterFailedStartAsync(container, profile);
                return Result<SandboxWorker>.Fail(SandboxErrors.PoolWorkerCreationFailed());
            }

            logger.LogInformation(
                "Запущен тёплый sandbox-контейнер {WorkerId} профиля {Profile} экземпляра {InstanceId}; длительность {ElapsedMs} мс",
                worker.WorkerId,
                profile.Key,
                instance.Id,
                Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
            return Result<SandboxWorker>.Success(worker);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await DisposeAfterFailedStartAsync(container, profile);
            throw;
        }
        catch (Exception exception)
        {
            await DisposeAfterFailedStartAsync(container, profile);
            logger.LogWarning(
                "Не удалось запустить тёплый sandbox-контейнер профиля {Profile}; тип сбоя {FailureType}",
                profile.Key,
                exception.GetType().Name);
            return Result<SandboxWorker>.Fail(SandboxErrors.PoolWorkerCreationFailed());
        }
    }

    public async ValueTask<Result> DeleteAsync(
        SandboxWorker worker,
        CancellationToken cancellationToken)
    {
        if (!containers.TryGetValue(worker.WorkerId, out var container))
        {
            return Result.Success();
        }

        try
        {
            await container.StopAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                "Не удалось штатно остановить тёплый sandbox-контейнер {WorkerId} профиля {Profile}; тип сбоя {FailureType}; выполняется принудительное удаление",
                worker.WorkerId,
                worker.Profile.Key,
                exception.GetType().Name);
        }

        try
        {
            await container.DisposeAsync();
            containers.TryRemove(worker.WorkerId, out _);
            logger.LogInformation(
                "Удалён тёплый sandbox-контейнер {WorkerId} профиля {Profile} экземпляра {InstanceId}",
                worker.WorkerId,
                worker.Profile.Key,
                instance.Id);
            return Result.Success();
        }
        catch (Exception exception)
        {
            logger.LogError(
                "Не удалось удалить тёплый sandbox-контейнер {WorkerId} профиля {Profile}; тип сбоя {FailureType}",
                worker.WorkerId,
                worker.Profile.Key,
                exception.GetType().Name);
            return Result.Fail(SandboxErrors.ContainerFailed("Sandbox worker container could not be deleted."));
        }
    }

    public async ValueTask<bool> IsHealthyAsync(
        SandboxWorker worker,
        CancellationToken cancellationToken)
    {
        if (!containers.TryGetValue(worker.WorkerId, out var container) ||
            container.State != TestcontainersStates.Running)
        {
            return false;
        }

        try
        {
            return await CanConnectAsync(worker, cancellationToken, retry: false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(
                "Проверка здоровья sandbox-воркера {WorkerId} профиля {Profile} завершилась сбоем типа {FailureType}",
                worker.WorkerId,
                worker.Profile.Key,
                exception.GetType().Name);
            return false;
        }
    }

    private async Task<bool> CanConnectAsync(
        SandboxWorker worker,
        CancellationToken cancellationToken,
        bool retry)
    {
        var dialect = dialectFactory.GetDialectFor(worker.Profile.Dbms.SystemName);
        if (dialect is null)
        {
            return false;
        }

        var dbms = worker.Profile.Dbms;
        var connectionString = dialect.BuildConnectionString(
            worker.Host,
            worker.MappedPort,
            dbms.DefaultDatabase,
            dbms.DefaultUsername,
            dbms.DefaultPassword);
        var attempts = retry ? 20 : 1;
        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                await using var connection = dialect.CreateConnection(connectionString);
                await connection.OpenAsync(cancellationToken);
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1";
                command.CommandTimeout = options.Value.Pool.HealthCheckIntervalSeconds;
                await command.ExecuteScalarAsync(cancellationToken);
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch when (attempt < attempts)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
            }
            catch
            {
                return false;
            }
        }

        return false;
    }

    private async ValueTask DisposeAfterFailedStartAsync(
        IContainer container,
        SandboxWorkerProfile profile)
    {
        try
        {
            await container.DisposeAsync();
        }
        catch (Exception exception)
        {
            logger.LogError(
                "Не удалось удалить неготовый sandbox-контейнер профиля {Profile}; тип сбоя {FailureType}",
                profile.Key,
                exception.GetType().Name);
        }
    }
}
