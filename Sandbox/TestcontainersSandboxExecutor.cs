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
            var applied = await ApplySetupAsync(conn, new SandboxSetup([ddlScript]), innerCt);
            return applied.IsSuccess
                ? await InspectCatalogAsync(conn, dbms.SystemName, innerCt)
                : Result<InspectedSchema>.Fail(applied.Error!);
        });
    }

    private static async Task<Result<InspectedSchema>> InspectCatalogAsync(
        DbConnection connection, string systemName, CancellationToken ct)
    {
        var mysql = systemName.Equals("mysql", StringComparison.OrdinalIgnoreCase) ||
                    systemName.Equals("mariadb", StringComparison.OrdinalIgnoreCase);
        var columnsSql = mysql
            ? """
              SELECT c.table_name, c.column_name, c.data_type, c.is_nullable,
                     c.ordinal_position, c.character_maximum_length, c.numeric_precision,
                     c.numeric_scale, CASE WHEN c.column_key = 'PRI' THEN 1 ELSE 0 END
              FROM information_schema.columns c WHERE c.table_schema = DATABASE()
              ORDER BY c.table_name, c.ordinal_position
              """
            : """
              SELECT c.table_name, c.column_name, c.data_type, c.is_nullable,
                     c.ordinal_position, c.character_maximum_length, c.numeric_precision,
                     c.numeric_scale, CASE WHEN EXISTS (
                       SELECT 1 FROM information_schema.table_constraints tc
                       JOIN information_schema.key_column_usage pk
                         ON pk.constraint_schema = tc.constraint_schema AND pk.constraint_name = tc.constraint_name
                       WHERE tc.constraint_type = 'PRIMARY KEY' AND tc.table_schema = c.table_schema
                         AND tc.table_name = c.table_name AND pk.column_name = c.column_name
                     ) THEN 1 ELSE 0 END
              FROM information_schema.columns c
              WHERE c.table_schema = current_schema()
              ORDER BY c.table_name, c.ordinal_position
              """;
        var relationshipsSql = mysql
            ? """
              SELECT rc.constraint_name, kcu.table_name, kcu.column_name,
                     kcu.referenced_table_name, kcu.referenced_column_name, rc.delete_rule, rc.update_rule
              FROM information_schema.referential_constraints rc
              JOIN information_schema.key_column_usage kcu
                ON kcu.constraint_schema = rc.constraint_schema AND kcu.constraint_name = rc.constraint_name
              WHERE rc.constraint_schema = DATABASE()
              ORDER BY rc.constraint_name, kcu.ordinal_position
              """
            : """
              SELECT tc.constraint_name, kcu.table_name, kcu.column_name,
                     ukcu.table_name, ukcu.column_name, rc.delete_rule, rc.update_rule
              FROM information_schema.table_constraints tc
              JOIN information_schema.key_column_usage kcu
                ON kcu.constraint_schema = tc.constraint_schema AND kcu.constraint_name = tc.constraint_name
              JOIN information_schema.referential_constraints rc
                ON rc.constraint_schema = tc.constraint_schema AND rc.constraint_name = tc.constraint_name
              JOIN information_schema.key_column_usage ukcu
                ON ukcu.constraint_schema = rc.unique_constraint_schema
               AND ukcu.constraint_name = rc.unique_constraint_name
               AND ukcu.ordinal_position = kcu.position_in_unique_constraint
              WHERE tc.constraint_type = 'FOREIGN KEY' AND tc.table_schema = current_schema()
              ORDER BY tc.constraint_name, kcu.ordinal_position
              """;

        try
        {
            var tableColumns = new Dictionary<string, List<InspectedColumn>>(StringComparer.OrdinalIgnoreCase);
            await using (var command = connection.CreateCommand())
            {
                command.CommandText = columnsSql;
                await using var reader = await command.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    var tableName = reader.GetString(0);
                    if (!tableColumns.TryGetValue(tableName, out var columns))
                    {
                        columns = [];
                        tableColumns[tableName] = columns;
                    }

                    columns.Add(new InspectedColumn(
                        reader.GetString(1), reader.GetString(2), reader.GetString(3) == "YES",
                        Convert.ToInt32(reader.GetValue(8)) == 1, Convert.ToInt32(reader.GetValue(4)) - 1,
                        reader.IsDBNull(5) ? null : Convert.ToInt32(reader.GetValue(5)),
                        reader.IsDBNull(6) ? null : Convert.ToInt32(reader.GetValue(6)),
                        reader.IsDBNull(7) ? null : Convert.ToInt32(reader.GetValue(7))));
                }
            }

            var relationships = new List<InspectedRelationship>();
            await using (var command = connection.CreateCommand())
            {
                command.CommandText = relationshipsSql;
                await using var reader = await command.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    relationships.Add(new InspectedRelationship(
                        reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                        reader.GetString(4), reader.GetString(5), reader.GetString(6)));
                }
            }

            return Result<InspectedSchema>.Success(new InspectedSchema(
                tableColumns.Select(x => new InspectedTable(x.Key, x.Value)).ToList(), relationships));
        }
        catch (Exception exception)
        {
            return Result<InspectedSchema>.Fail(SandboxErrors.SetupFailed(exception.Message));
        }
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
            var isTruncated = false;
            while (await reader.ReadAsync(ct))
            {
                if (rows.Count >= query.MaxRows)
                {
                    isTruncated = true;
                    break;
                }

                var row = new string?[reader.FieldCount];
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    row[i] = reader.IsDBNull(i)
                        ? null
                        : Convert.ToString(reader.GetValue(i), System.Globalization.CultureInfo.InvariantCulture);
                }

                rows.Add(row);
            }

            sw.Stop();
            return Result<QueryResultSet>.Success(
                new QueryResultSet(true, null, columns, rows, rows.Count, sw.ElapsedMilliseconds, isTruncated));
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
