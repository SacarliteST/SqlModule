using System.Data.Common;
using System.Diagnostics;
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
            var setupResult = await ApplySetupAsync(conn, setup, innerCt);
            if (!setupResult.IsSuccess)
            {
                return Result<QueryResultSet>.Fail(setupResult.Error!);
            }

            return await ExecuteQueryAsync(dialect, conn, query, innerCt);
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
            var setupResult = await ApplySetupAsync(conn, setup, innerCt);
            return setupResult.IsSuccess
                ? Result<bool>.Success(true)
                : Result<bool>.Fail(setupResult.Error!);
        });
        return r.IsSuccess ? Result.Success() : Result.Fail(r.Error!);
    }

    private async Task<Result<T>> RunInContainerAsync<T>(
        SandboxDbmsSpec dbms,
        ISqlDialect dialect,
        CancellationToken ct,
        Func<DbConnection, CancellationToken, Task<Result<T>>> body)
    {
        var envVars = BuildEnvVars(dbms);

        await using var container = new ContainerBuilder()
            .WithImage(dbms.DockerImage)
            .WithPortBinding(dbms.DefaultPort, true)
            .WithEnvironment(envVars)
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
            await conn.OpenAsync(ct);
        }
        catch (Exception ex)
        {
            return Result<T>.Fail(SandboxErrors.ContainerFailed(ex.Message));
        }

        return await body(conn, ct);
    }

    private async Task<Result> ApplySetupAsync(DbConnection conn, SandboxSetup setup, CancellationToken ct)
    {
        foreach (var statement in setup.Statements)
        {
            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = statement;
                cmd.CommandTimeout = opts.DefaultQueryTimeoutSeconds;
                await cmd.ExecuteNonQueryAsync(ct);
            }
            catch (Exception ex)
            {
                return Result.Fail(SandboxErrors.SetupFailed(ex.Message));
            }
        }
        return Result.Success();
    }

    private static async Task<Result<QueryResultSet>> ExecuteQueryAsync(
        ISqlDialect dialect, DbConnection conn, SandboxQuery query, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await dialect.BeginReadOnlyAsync(conn, ct);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = query.Sql;
            cmd.CommandTimeout = query.TimeoutSeconds;

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            var columns = Enumerable.Range(0, reader.FieldCount)
                .Select(i => reader.GetName(i))
                .ToList();

            var rows = new List<IReadOnlyList<string?>>();
            while (await reader.ReadAsync(ct) && rows.Count < query.MaxRows)
            {
                var row = new string?[reader.FieldCount];
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    row[i] = reader.IsDBNull(i) ? null : reader.GetValue(i).ToString();
                }

                rows.Add(row);
            }

            sw.Stop();
            return Result<QueryResultSet>.Success(
                new QueryResultSet(true, null, columns, rows, rows.Count, sw.ElapsedMilliseconds));
        }
        catch (Exception ex)
        {
            sw.Stop();
            return Result<QueryResultSet>.Success(
                new QueryResultSet(false, ex.Message, [], [], 0, sw.ElapsedMilliseconds));
        }
        finally
        {
            try
            {
                await using var rollback = conn.CreateCommand();
                rollback.CommandText = "ROLLBACK";
                await rollback.ExecuteNonQueryAsync(CancellationToken.None);
            }
            catch
            {
                // игнорируем ошибку отката
            }
        }
    }

    private static Dictionary<string, string> BuildEnvVars(SandboxDbmsSpec dbms)
    {
        var env = new Dictionary<string, string>
        {
            [dbms.EnvUserKey] = dbms.DefaultUsername,
            [dbms.EnvPasswordKey] = dbms.DefaultPassword,
        };

        if (dbms.EnvDatabaseKey is not null)
        {
            env[dbms.EnvDatabaseKey] = dbms.DefaultDatabase;
        }

        if (String.IsNullOrWhiteSpace(dbms.ExtraEnvConfig))
        {
            return env;
        }

        foreach (var pair in dbms.ExtraEnvConfig.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = pair.IndexOf('=');
            if (idx > 0)
            {
                env[pair[..idx].Trim()] = pair[(idx + 1)..].Trim();
            }
        }

        return env;
    }
}
