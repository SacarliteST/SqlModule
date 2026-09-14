using System.Data.Common;
using System.Diagnostics;
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
            var preparationStartedAt = Stopwatch.GetTimestamp();
            using var preparation = CreatePreparationTimeout(ct);
            var setupResult = await ApplySetupAsync(
                lease,
                sandboxNamespace,
                setup,
                preparation.Token);
            if (!setupResult.IsSuccess)
            {
                RecordPreparation(lease, preparationStartedAt, "failure");
                return Result<QueryResultSet>.Fail(setupResult.Error!);
            }

            var grantResult = await isolationManager.GrantRunnerAccessAsync(
                lease,
                sandboxNamespace,
                preparation.Token);
            if (!grantResult.IsSuccess)
            {
                RecordPreparation(lease, preparationStartedAt, "failure");
                return Result<QueryResultSet>.Fail(grantResult.Error!);
            }

            var connectionResult = await isolationManager.OpenRunnerConnectionAsync(lease, sandboxNamespace, ct);
            if (!connectionResult.IsSuccess)
            {
                RecordPreparation(lease, preparationStartedAt, "failure");
                return Result<QueryResultSet>.Fail(connectionResult.Error!);
            }

            var runner = connectionResult.Value!;
            RecordPreparation(lease, preparationStartedAt, "success");
            var executionStartedAt = Stopwatch.GetTimestamp();
            Result<QueryResultSet> result;
            try
            {
                result = await SandboxDatabaseOperations.ExecuteQueryAsync(
                    dialect,
                    runner,
                    query,
                    ct,
                    lease.Discard);
            }
            catch (OperationCanceledException)
            {
                SandboxPoolTelemetry.RecordExecution(
                    lease.Worker.Profile.Key.SystemName,
                    Stopwatch.GetElapsedTime(executionStartedAt),
                    "canceled");
                throw;
            }
            finally
            {
                await DisposeConnectionAsync(runner, lease, "runner");
            }

            SandboxPoolTelemetry.RecordExecution(
                lease.Worker.Profile.Key.SystemName,
                Stopwatch.GetElapsedTime(executionStartedAt),
                result.IsSuccess ? "success" : "failure");
            logger.LogDebug(
                "Завершено выполнение SQL в sandbox lease {LeaseId}, worker {WorkerId}, профиль {Profile}; результат {Outcome}, длительность {ElapsedMs} мс",
                lease.LeaseId,
                lease.Worker.WorkerId,
                lease.Worker.Profile.Key,
                result.IsSuccess ? "успех" : "ошибка",
                Stopwatch.GetElapsedTime(executionStartedAt).TotalMilliseconds);
            return result;
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
                var preparationStartedAt = Stopwatch.GetTimestamp();
                using var preparation = CreatePreparationTimeout(ct);
                var setupResult = await ApplySetupAsync(
                    lease,
                    sandboxNamespace,
                    setup,
                    preparation.Token);
                RecordPreparation(
                    lease,
                    preparationStartedAt,
                    setupResult.IsSuccess ? "success" : "failure");
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
            var preparationStartedAt = Stopwatch.GetTimestamp();
            using var preparation = CreatePreparationTimeout(ct);
            var connectionResult = await isolationManager.OpenSetupConnectionAsync(
                lease,
                sandboxNamespace,
                preparation.Token);
            if (!connectionResult.IsSuccess)
            {
                RecordPreparation(lease, preparationStartedAt, "failure");
                return Result<InspectedSchema>.Fail(connectionResult.Error!);
            }

            var setupConnection = connectionResult.Value!;
            try
            {
                var setupResult = await SandboxDatabaseOperations.ApplySetupAsync(
                    setupConnection,
                    new SandboxSetup([ddlScript]),
                    sandboxOptions.DefaultQueryTimeoutSeconds,
                    preparation.Token,
                    lease.Discard);
                var result = setupResult.IsSuccess
                    ? await SandboxDatabaseOperations.InspectCatalogAsync(
                        setupConnection,
                        dbms.SystemName,
                        preparation.Token,
                        lease.Discard)
                    : Result<InspectedSchema>.Fail(setupResult.Error!);
                RecordPreparation(
                    lease,
                    preparationStartedAt,
                    result.IsSuccess ? "success" : "failure");
                return result;
            }
            finally
            {
                await DisposeConnectionAsync(setupConnection, lease, "setup");
            }
        });

    private async Task<Result> ApplySetupAsync(
        SandboxLease lease,
        SandboxIsolationNamespace sandboxNamespace,
        SandboxSetup setup,
        CancellationToken ct)
    {
        var connectionResult = await isolationManager.OpenSetupConnectionAsync(
            lease,
            sandboxNamespace,
            ct);
        if (!connectionResult.IsSuccess)
        {
            return Result.Fail(connectionResult.Error!);
        }

        var setupConnection = connectionResult.Value!;
        try
        {
            return await SandboxDatabaseOperations.ApplySetupAsync(
                setupConnection,
                setup,
                sandboxOptions.DefaultQueryTimeoutSeconds,
                ct,
                lease.Discard);
        }
        finally
        {
            await DisposeConnectionAsync(setupConnection, lease, "setup");
        }
    }

    private async Task<Result<T>> ExecuteIsolatedAsync<T>(
        SandboxDbmsSpec dbms,
        CancellationToken ct,
        Func<SandboxLease, SandboxIsolationNamespace, ISqlDialect, Task<Result<T>>> operation)
    {
        var profile = dbms.SystemName.Trim().ToLowerInvariant();
        var startedAt = Stopwatch.GetTimestamp();
        SandboxPoolTelemetry.OperationStarted(profile);
        try
        {
            var result = await ExecuteIsolatedCoreAsync(dbms, ct, operation);
            SandboxPoolTelemetry.RecordCycle(
                profile,
                Stopwatch.GetElapsedTime(startedAt),
                result.IsSuccess ? "success" : "failure");
            logger.LogDebug(
                "Завершён полный цикл sandbox профиля {Profile}; результат {Outcome}, длительность {ElapsedMs} мс",
                profile,
                result.IsSuccess ? "успех" : "ошибка",
                Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
            return result;
        }
        catch (OperationCanceledException)
        {
            SandboxPoolTelemetry.RecordCycle(profile, Stopwatch.GetElapsedTime(startedAt), "canceled");
            throw;
        }
        finally
        {
            SandboxPoolTelemetry.OperationFinished(profile);
        }
    }

    private async Task<Result<T>> ExecuteIsolatedCoreAsync<T>(
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
        Result<SandboxIsolationNamespace> namespaceResult;
        using (var preparation = CreatePreparationTimeout(ct))
        {
            var preparationStartedAt = Stopwatch.GetTimestamp();
            try
            {
                namespaceResult = await isolationManager.CreateAsync(lease, preparation.Token);
                RecordPreparation(
                    lease,
                    preparationStartedAt,
                    namespaceResult.IsSuccess ? "success" : "failure");
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                RecordPreparation(lease, preparationStartedAt, "timeout");
                lease.Discard();
                return Result<T>.Fail(SandboxErrors.PreparationTimeout());
            }
            catch (OperationCanceledException)
            {
                RecordPreparation(lease, preparationStartedAt, "canceled");
                lease.Discard();
                throw;
            }
        }
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
            if (ct.IsCancellationRequested)
            {
                throw;
            }

            return Result<T>.Fail(SandboxErrors.PreparationTimeout());
        }
        catch (Exception exception)
        {
            lease.Discard();
            logger.LogError(
                "Неожиданный сбой операции в namespace {NamespaceId} на sandbox-воркере {WorkerId}; тип сбоя {FailureType}",
                sandboxNamespace.Id,
                lease.Worker.WorkerId,
                exception.GetType().Name);
            return Result<T>.Fail(SandboxErrors.ContainerFailed("Sandbox pooled operation failed."));
        }

        if (lease.Disposition == SandboxLeaseDisposition.Discard)
        {
            return operationResult;
        }

        var cleanupResult = await isolationManager.CleanupAsync(
            lease,
            sandboxNamespace,
            CancellationToken.None);
        return cleanupResult.IsSuccess
            ? operationResult
            : Result<T>.Fail(cleanupResult.Error!);
    }

    private CancellationTokenSource CreatePreparationTimeout(CancellationToken ct)
    {
        var preparation = CancellationTokenSource.CreateLinkedTokenSource(ct);
        preparation.CancelAfter(TimeSpan.FromSeconds(sandboxOptions.Pool.PreparationTimeoutSeconds));
        return preparation;
    }

    private async Task DisposeConnectionAsync(
        DbConnection connection,
        SandboxLease lease,
        string connectionKind)
    {
        try
        {
            await connection.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (Exception exception)
        {
            lease.Discard();
            logger.LogWarning(
                "Не удалось вовремя закрыть {ConnectionKind}-соединение для sandbox lease {LeaseId}, worker {WorkerId}; контейнер будет заменён, тип сбоя {FailureType}",
                connectionKind,
                lease.LeaseId,
                lease.Worker.WorkerId,
                exception.GetType().Name);
        }
    }

    private void RecordPreparation(SandboxLease lease, long startedAt, string outcome)
    {
        var elapsed = Stopwatch.GetElapsedTime(startedAt);
        SandboxPoolTelemetry.RecordPreparation(
            lease.Worker.Profile.Key.SystemName,
            elapsed,
            outcome);
        logger.LogDebug(
            "Завершена подготовка sandbox lease {LeaseId}, worker {WorkerId}, профиль {Profile}; результат {Outcome}, длительность {ElapsedMs} мс",
            lease.LeaseId,
            lease.Worker.WorkerId,
            lease.Worker.Profile.Key,
            outcome,
            elapsed.TotalMilliseconds);
    }
}
