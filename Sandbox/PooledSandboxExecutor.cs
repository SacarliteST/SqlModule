using System.Data.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SQLModule.Common.Results;
using SQLModule.Sandbox.Dialects;
using SQLModule.Sandbox.Pooling;

namespace SQLModule.Sandbox;

internal sealed class PooledSandboxExecutor(
    ISqlDialectFactory dialectFactory,
    ISandboxLeaseManager leaseManager,
    ISandboxIsolationManager isolationManager,
    IOptions<SandboxOptions> options,
    ILogger<PooledSandboxExecutor> logger) : ISandboxExecutor
{
    private readonly SandboxOptions sandboxOptions = options.Value;

    public Task<Result<QueryResultSet>> RunAsync(
        SandboxDbmsSpec dbms,
        SandboxSetup setup,
        SandboxQuery query,
        CancellationToken ct) => ExecuteIsolatedAsync(
        dbms,
        ct,
        async (lease, sandboxNamespace, dialect) =>
        {
            var setupResult = await ApplySetupAsync(lease, sandboxNamespace, setup, ct);
            if (!setupResult.IsSuccess)
            {
                return Result<QueryResultSet>.Fail(setupResult.Error!);
            }

            var grantResult = await isolationManager.GrantRunnerAccessAsync(lease, sandboxNamespace, ct);
            if (!grantResult.IsSuccess)
            {
                return Result<QueryResultSet>.Fail(grantResult.Error!);
            }

            var connectionResult = await isolationManager.OpenRunnerConnectionAsync(lease, sandboxNamespace, ct);
            if (!connectionResult.IsSuccess)
            {
                return Result<QueryResultSet>.Fail(connectionResult.Error!);
            }

            await using var runner = connectionResult.Value!;
            return await SandboxDatabaseOperations.ExecuteQueryAsync(dialect, runner, query, ct);
        });

    public async Task<Result> ValidateSetupAsync(
        SandboxDbmsSpec dbms,
        SandboxSetup setup,
        CancellationToken ct)
    {
        var result = await ExecuteIsolatedAsync(
            dbms,
            ct,
            async (lease, sandboxNamespace, _) =>
            {
                var setupResult = await ApplySetupAsync(lease, sandboxNamespace, setup, ct);
                return setupResult.IsSuccess
                    ? Result<bool>.Success(true)
                    : Result<bool>.Fail(setupResult.Error!);
            });
        return result.IsSuccess ? Result.Success() : Result.Fail(result.Error!);
    }

    public Task<Result<InspectedSchema>> InspectDdlAsync(
        SandboxDbmsSpec dbms,
        string ddlScript,
        CancellationToken ct) => ExecuteIsolatedAsync(
        dbms,
        ct,
        async (lease, sandboxNamespace, _) =>
        {
            using var preparation = CancellationTokenSource.CreateLinkedTokenSource(ct);
            preparation.CancelAfter(TimeSpan.FromSeconds(sandboxOptions.Pool.PreparationTimeoutSeconds));
            var connectionResult = await isolationManager.OpenSetupConnectionAsync(
                lease,
                sandboxNamespace,
                preparation.Token);
            if (!connectionResult.IsSuccess)
            {
                return Result<InspectedSchema>.Fail(connectionResult.Error!);
            }

            await using var setupConnection = connectionResult.Value!;
            var setupResult = await SandboxDatabaseOperations.ApplySetupAsync(
                setupConnection,
                new SandboxSetup([ddlScript]),
                sandboxOptions.DefaultQueryTimeoutSeconds,
                preparation.Token);
            return setupResult.IsSuccess
                ? await SandboxDatabaseOperations.InspectCatalogAsync(
                    setupConnection,
                    dbms.SystemName,
                    preparation.Token)
                : Result<InspectedSchema>.Fail(setupResult.Error!);
        });

    private async Task<Result> ApplySetupAsync(
        SandboxLease lease,
        SandboxIsolationNamespace sandboxNamespace,
        SandboxSetup setup,
        CancellationToken ct)
    {
        using var preparation = CancellationTokenSource.CreateLinkedTokenSource(ct);
        preparation.CancelAfter(TimeSpan.FromSeconds(sandboxOptions.Pool.PreparationTimeoutSeconds));
        var connectionResult = await isolationManager.OpenSetupConnectionAsync(
            lease,
            sandboxNamespace,
            preparation.Token);
        if (!connectionResult.IsSuccess)
        {
            return Result.Fail(connectionResult.Error!);
        }

        await using var setupConnection = connectionResult.Value!;
        return await SandboxDatabaseOperations.ApplySetupAsync(
            setupConnection,
            setup,
            sandboxOptions.DefaultQueryTimeoutSeconds,
            preparation.Token);
    }

    private async Task<Result<T>> ExecuteIsolatedAsync<T>(
        SandboxDbmsSpec dbms,
        CancellationToken ct,
        Func<SandboxLease, SandboxIsolationNamespace, ISqlDialect, Task<Result<T>>> operation)
    {
        var dialect = dialectFactory.GetDialectFor(dbms.SystemName);
        if (dialect is null)
        {
            return Result<T>.Fail(SandboxErrors.UnsupportedDbms(dbms.SystemName));
        }

        var limits = SandboxPoolProfileResolver.Find(sandboxOptions.Pool, dbms.SystemName);
        if (limits is null)
        {
            return Result<T>.Fail(SandboxErrors.PoolProfileNotConfigured(dbms.SystemName));
        }

        var leaseResult = await leaseManager.AcquireAsync(new SandboxWorkerProfile(dbms, limits), ct);
        if (!leaseResult.IsSuccess)
        {
            return Result<T>.Fail(leaseResult.Error!);
        }

        await using var lease = leaseResult.Value!;
        var namespaceResult = await isolationManager.CreateAsync(lease, ct);
        if (!namespaceResult.IsSuccess)
        {
            return Result<T>.Fail(namespaceResult.Error!);
        }

        var sandboxNamespace = namespaceResult.Value!;
        Result<T> operationResult;
        try
        {
            operationResult = await operation(lease, sandboxNamespace, dialect);
        }
        catch (OperationCanceledException)
        {
            lease.Discard();
            await isolationManager.CleanupAsync(lease, sandboxNamespace, CancellationToken.None);
            throw;
        }
        catch (Exception exception)
        {
            lease.Discard();
            logger.LogError(
                "Неожиданный сбой операции в namespace {NamespaceId} на sandbox-воркере {WorkerId}; тип сбоя {FailureType}",
                sandboxNamespace.Id,
                lease.Worker.WorkerId,
                exception.GetType().Name);
            await isolationManager.CleanupAsync(lease, sandboxNamespace, CancellationToken.None);
            return Result<T>.Fail(SandboxErrors.ContainerFailed("Sandbox pooled operation failed."));
        }

        var cleanupResult = await isolationManager.CleanupAsync(
            lease,
            sandboxNamespace,
            CancellationToken.None);
        return cleanupResult.IsSuccess
            ? operationResult
            : Result<T>.Fail(cleanupResult.Error!);
    }
}
