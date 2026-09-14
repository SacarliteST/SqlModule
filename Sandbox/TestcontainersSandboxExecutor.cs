using System.Data.Common;
using DotNet.Testcontainers.Builders;
using Microsoft.Extensions.Options;
using SQLModule.Common.Results;
using SQLModule.Sandbox.Dialects;

namespace SQLModule.Sandbox;

internal sealed class TestcontainersSandboxExecutor(
    ISqlDialectFactory dialectFactory,
    IOptions<SandboxOptions> options) : ISandboxExecutor
{
    private readonly SandboxOptions opts = options.Value;

    public async Task<Result<QueryResultSet>> RunAsync(
        SandboxDbmsSpec dbms, SandboxSetup setup, SandboxQuery query, CancellationToken ct)
    {
        var dialect = dialectFactory.GetDialectFor(dbms.SystemName);
        if (dialect is null)
        {
            return Result<QueryResultSet>.Fail(SandboxErrors.UnsupportedDbms(dbms.SystemName));
        }

        return await RunInContainerAsync(dbms, dialect, ct, async (conn, innerCt) =>
        {
            var setupResult = await SandboxDatabaseOperations.ApplySetupAsync(
                conn,
                setup,
                opts.DefaultQueryTimeoutSeconds,
                innerCt);
            if (!setupResult.IsSuccess)
            {
                return Result<QueryResultSet>.Fail(setupResult.Error!);
            }

            return await SandboxDatabaseOperations.ExecuteQueryAsync(dialect, conn, query, innerCt);
        });
    }

    public async Task<Result> ValidateSetupAsync(
        SandboxDbmsSpec dbms, SandboxSetup setup, CancellationToken ct)
    {
        var dialect = dialectFactory.GetDialectFor(dbms.SystemName);
        if (dialect is null)
        {
            return Result.Fail(SandboxErrors.UnsupportedDbms(dbms.SystemName));
        }

        var r = await RunInContainerAsync<bool>(dbms, dialect, ct, async (conn, innerCt) =>
        {
            var setupResult = await SandboxDatabaseOperations.ApplySetupAsync(
                conn,
                setup,
                opts.DefaultQueryTimeoutSeconds,
                innerCt);
            return setupResult.IsSuccess
                ? Result<bool>.Success(true)
                : Result<bool>.Fail(setupResult.Error!);
        });
        return r.IsSuccess ? Result.Success() : Result.Fail(r.Error!);
    }

    public async Task<Result<InspectedSchema>> InspectDdlAsync(
        SandboxDbmsSpec dbms, string ddlScript, CancellationToken ct)
    {
        var dialect = dialectFactory.GetDialectFor(dbms.SystemName);
        if (dialect is null)
        {
            return Result<InspectedSchema>.Fail(SandboxErrors.UnsupportedDbms(dbms.SystemName));
        }

        return await RunInContainerAsync(dbms, dialect, ct, async (conn, innerCt) =>
        {
            var applied = await SandboxDatabaseOperations.ApplySetupAsync(
                conn,
                new SandboxSetup([ddlScript]),
                opts.DefaultQueryTimeoutSeconds,
                innerCt);
            return applied.IsSuccess
                ? await SandboxDatabaseOperations.InspectCatalogAsync(conn, dbms.SystemName, innerCt)
                : Result<InspectedSchema>.Fail(applied.Error!);
        });
    }

    private async Task<Result<T>> RunInContainerAsync<T>(
        SandboxDbmsSpec dbms,
        ISqlDialect dialect,
        CancellationToken ct,
        Func<DbConnection, CancellationToken, Task<Result<T>>> body)
    {
        await using var container = new ContainerBuilder()
            .WithImage(dbms.DockerImage)
            .WithPortBinding(dbms.DefaultPort, true)
            .WithEnvironment(SandboxContainerEnvironment.Build(dbms))
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(dbms.DefaultPort))
            .Build();

        using var startCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        startCts.CancelAfter(TimeSpan.FromSeconds(opts.ContainerStartupTimeoutSeconds));

        try
        {
            await container.StartAsync(startCts.Token);
        }
        catch (Exception ex)
        {
            return Result<T>.Fail(SandboxErrors.ContainerFailed(ex.Message));
        }

        var host = container.Hostname;
        var port = container.GetMappedPublicPort(dbms.DefaultPort);
        var connString = dialect.BuildConnectionString(
            host, port, dbms.DefaultDatabase, dbms.DefaultUsername, dbms.DefaultPassword);

        await using var conn = dialect.CreateConnection(connString);
        try
        {
            await OpenWithRetryAsync(conn, ct);
        }
        catch (Exception ex)
        {
            return Result<T>.Fail(SandboxErrors.ContainerFailed(ex.Message));
        }

        return await body(conn, ct);
    }

    private static async Task OpenWithRetryAsync(DbConnection connection, CancellationToken ct)
    {
        Exception? lastError = null;
        for (var attempt = 0; attempt < 20; attempt++)
        {
            try
            {
                await connection.OpenAsync(ct);
                return;
            }
            catch (Exception exception) when (attempt < 19 && !ct.IsCancellationRequested)
            {
                lastError = exception;
                await Task.Delay(TimeSpan.FromMilliseconds(250), ct);
            }
        }

        throw lastError ?? new InvalidOperationException("Не удалось подключиться к sandbox СУБД.");
    }

}
