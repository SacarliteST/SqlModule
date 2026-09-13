using System.Data.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SQLModule.Common.Results;
using SQLModule.Sandbox.Dialects;

namespace SQLModule.Sandbox.Pooling;

internal sealed class SandboxIsolationManager(
    ISqlDialectFactory dialectFactory,
    IOptions<SandboxOptions> options,
    ILogger<SandboxIsolationManager> logger) : ISandboxIsolationManager
{
    public async Task<Result<SandboxIsolationNamespace>> CreateAsync(
        SandboxLease lease,
        CancellationToken ct)
    {
        var sandboxNamespace = SandboxIsolationNamespace.Create();
        var dialect = GetDialect(lease);
        if (dialect is null)
        {
            return Result<SandboxIsolationNamespace>.Fail(
                SandboxErrors.UnsupportedDbms(lease.Worker.Profile.Dbms.SystemName));
        }

        try
        {
            await using var control = await OpenControlConnectionAsync(lease, dialect, ct);
            await dialect.CreateIsolationNamespaceAsync(control, sandboxNamespace, ct);
            logger.LogInformation(
                "Создан изолированный namespace {NamespaceId} на sandbox-воркере {WorkerId} для lease {LeaseId}",
                sandboxNamespace.Id,
                lease.Worker.WorkerId,
                lease.LeaseId);
            return Result<SandboxIsolationNamespace>.Success(sandboxNamespace);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            await CleanupAfterFailedCreationAsync(lease, dialect, sandboxNamespace);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                "Не удалось создать изолированный namespace {NamespaceId} на sandbox-воркере {WorkerId}; тип сбоя {FailureType}",
                sandboxNamespace.Id,
                lease.Worker.WorkerId,
                exception.GetType().Name);
            await CleanupAfterFailedCreationAsync(lease, dialect, sandboxNamespace);
            return Result<SandboxIsolationNamespace>.Fail(SandboxErrors.IsolationPreparationFailed());
        }
    }

    public Task<Result<DbConnection>> OpenSetupConnectionAsync(
        SandboxLease lease,
        SandboxIsolationNamespace sandboxNamespace,
        CancellationToken ct) => OpenRestrictedConnectionAsync(
            lease,
            sandboxNamespace,
            sandboxNamespace.SetupUsername,
            sandboxNamespace.SetupPassword,
            ct);

    public async Task<Result> GrantRunnerAccessAsync(
        SandboxLease lease,
        SandboxIsolationNamespace sandboxNamespace,
        CancellationToken ct)
    {
        var dialect = GetDialect(lease);
        if (dialect is null)
        {
            return Result.Fail(SandboxErrors.UnsupportedDbms(lease.Worker.Profile.Dbms.SystemName));
        }

        try
        {
            var connectionResult = await OpenSetupConnectionAsync(lease, sandboxNamespace, ct);
            if (!connectionResult.IsSuccess)
            {
                return Result.Fail(connectionResult.Error!);
            }

            await using var setupConnection = connectionResult.Value!;
            await dialect.GrantRunnerAccessAsync(setupConnection, sandboxNamespace, ct);
            return Result.Success();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(
                "Не удалось настроить права runner для namespace {NamespaceId} на sandbox-воркере {WorkerId}; тип сбоя {FailureType}",
                sandboxNamespace.Id,
                lease.Worker.WorkerId,
                exception.GetType().Name);
            return Result.Fail(SandboxErrors.IsolationPreparationFailed());
        }
    }

    public Task<Result<DbConnection>> OpenRunnerConnectionAsync(
        SandboxLease lease,
        SandboxIsolationNamespace sandboxNamespace,
        CancellationToken ct) => OpenRestrictedConnectionAsync(
            lease,
            sandboxNamespace,
            sandboxNamespace.RunnerUsername,
            sandboxNamespace.RunnerPassword,
            ct);

    public async Task<Result> CleanupAsync(
        SandboxLease lease,
        SandboxIsolationNamespace sandboxNamespace,
        CancellationToken _)
    {
        var dialect = GetDialect(lease);
        if (dialect is null)
        {
            lease.Discard();
            return Result.Fail(SandboxErrors.UnsupportedDbms(lease.Worker.Profile.Dbms.SystemName));
        }

        using var cleanup = new CancellationTokenSource(
            TimeSpan.FromSeconds(options.Value.Pool.CleanupTimeoutSeconds));
        try
        {
            await using var control = await OpenControlConnectionAsync(lease, dialect, cleanup.Token);
            await dialect.CleanupIsolationNamespaceAsync(control, sandboxNamespace, cleanup.Token);
            if (await dialect.IsolationNamespaceExistsAsync(control, sandboxNamespace, cleanup.Token))
            {
                throw new InvalidOperationException("Sandbox namespace still exists after cleanup.");
            }

            logger.LogInformation(
                "Очищен изолированный namespace {NamespaceId} на sandbox-воркере {WorkerId} для lease {LeaseId}",
                sandboxNamespace.Id,
                lease.Worker.WorkerId,
                lease.LeaseId);
            return Result.Success();
        }
        catch (Exception exception)
        {
            lease.Discard();
            logger.LogError(
                "Не удалось гарантированно очистить namespace {NamespaceId} на sandbox-воркере {WorkerId}; воркер будет удалён; тип сбоя {FailureType}",
                sandboxNamespace.Id,
                lease.Worker.WorkerId,
                exception.GetType().Name);
            return Result.Fail(SandboxErrors.IsolationCleanupFailed());
        }
    }

    private async Task<Result<DbConnection>> OpenRestrictedConnectionAsync(
        SandboxLease lease,
        SandboxIsolationNamespace sandboxNamespace,
        string username,
        string password,
        CancellationToken ct)
    {
        var dialect = GetDialect(lease);
        if (dialect is null)
        {
            return Result<DbConnection>.Fail(SandboxErrors.UnsupportedDbms(lease.Worker.Profile.Dbms.SystemName));
        }

        DbConnection? connection = null;
        try
        {
            var connectionString = dialect.BuildConnectionString(
                lease.Worker.Host,
                lease.Worker.MappedPort,
                sandboxNamespace.DatabaseName,
                username,
                password);
            connection = dialect.CreateConnection(connectionString);
            await connection.OpenAsync(ct);
            return Result<DbConnection>.Success(connection);
        }
        catch (OperationCanceledException)
        {
            if (connection is not null)
            {
                await connection.DisposeAsync();
            }

            throw;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            if (connection is not null)
            {
                await connection.DisposeAsync();
            }

            logger.LogWarning(
                "Не удалось открыть ограниченное подключение к namespace {NamespaceId} на sandbox-воркере {WorkerId}; тип сбоя {FailureType}",
                sandboxNamespace.Id,
                lease.Worker.WorkerId,
                exception.GetType().Name);
            return Result<DbConnection>.Fail(SandboxErrors.IsolationPreparationFailed());
        }
    }

    private async Task<DbConnection> OpenControlConnectionAsync(
        SandboxLease lease,
        ISqlDialect dialect,
        CancellationToken ct)
    {
        var dbms = lease.Worker.Profile.Dbms;
        var connectionString = dialect.BuildControlConnectionString(
            lease.Worker.Host,
            lease.Worker.MappedPort,
            dbms);
        var connection = dialect.CreateConnection(connectionString);
        try
        {
            await connection.OpenAsync(ct);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    private ISqlDialect? GetDialect(SandboxLease lease) =>
        dialectFactory.GetDialectFor(lease.Worker.Profile.Dbms.SystemName);

    private async Task CleanupAfterFailedCreationAsync(
        SandboxLease lease,
        ISqlDialect dialect,
        SandboxIsolationNamespace sandboxNamespace)
    {
        using var cleanup = new CancellationTokenSource(
            TimeSpan.FromSeconds(options.Value.Pool.CleanupTimeoutSeconds));
        try
        {
            await using var control = await OpenControlConnectionAsync(lease, dialect, cleanup.Token);
            await dialect.CleanupIsolationNamespaceAsync(control, sandboxNamespace, cleanup.Token);
            if (await dialect.IsolationNamespaceExistsAsync(control, sandboxNamespace, cleanup.Token))
            {
                lease.Discard();
            }
        }
        catch
        {
            lease.Discard();
        }
    }
}
