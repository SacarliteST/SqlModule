using System.Data;
using System.Data.Common;
using System.Diagnostics;
using MySqlConnector;
using Npgsql;
using SQLModule.Common.Results;
using SQLModule.Sandbox.Dialects;

namespace SQLModule.Sandbox;

internal static class SandboxDatabaseOperations
{
    internal static async Task<Result> ApplySetupAsync(
        DbConnection connection,
        SandboxSetup setup,
        int commandTimeoutSeconds,
        CancellationToken ct,
        Action? quarantineWorker = null)
    {
        foreach (var statement in setup.Statements)
        {
            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = statement;
                command.CommandTimeout = commandTimeoutSeconds;
                using var cancellation = CancelCommandOnCancellation(command, ct);
                await command.ExecuteNonQueryAsync(ct).WaitAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                quarantineWorker?.Invoke();
                throw;
            }
            catch (Exception exception)
            {
                if (RequiresQuarantine(connection, exception))
                {
                    quarantineWorker?.Invoke();
                }

                return Result.Fail(SandboxErrors.SetupFailed(exception.Message));
            }
        }

        return Result.Success();
    }

    internal static async Task<Result<QueryResultSet>> ExecuteQueryAsync(
        ISqlDialect dialect,
        DbConnection connection,
        SandboxQuery query,
        CancellationToken ct,
        Action? quarantineWorker = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var requiresQuarantine = false;
        using var queryTimeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        queryTimeout.CancelAfter(TimeSpan.FromSeconds(query.TimeoutSeconds));
        var operationToken = queryTimeout.Token;
        try
        {
            await dialect.BeginReadOnlyAsync(connection, operationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = query.Sql;
            command.CommandTimeout = query.TimeoutSeconds;
            using var cancellation = CancelCommandOnCancellation(command, operationToken);

            await using var reader = await command.ExecuteReaderAsync(operationToken).WaitAsync(operationToken);
            var columns = Enumerable.Range(0, reader.FieldCount)
                .Select(reader.GetName)
                .ToList();
            var rows = new List<IReadOnlyList<string?>>();
            var isTruncated = false;
            while (await reader.ReadAsync(operationToken).WaitAsync(operationToken))
            {
                if (rows.Count >= query.MaxRows)
                {
                    isTruncated = true;
                    break;
                }

                var row = new string?[reader.FieldCount];
                for (var index = 0; index < reader.FieldCount; index++)
                {
                    row[index] = reader.IsDBNull(index)
                        ? null
                        : Convert.ToString(
                            reader.GetValue(index),
                            System.Globalization.CultureInfo.InvariantCulture);
                }

                rows.Add(row);
            }

            stopwatch.Stop();
            return Result<QueryResultSet>.Success(new QueryResultSet(
                true,
                null,
                columns,
                rows,
                rows.Count,
                stopwatch.ElapsedMilliseconds,
                isTruncated));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            requiresQuarantine = true;
            quarantineWorker?.Invoke();
            throw;
        }
        catch (OperationCanceledException)
        {
            requiresQuarantine = true;
            quarantineWorker?.Invoke();
            stopwatch.Stop();
            return Result<QueryResultSet>.Success(new QueryResultSet(
                false,
                "Query execution timed out.",
                [],
                [],
                0,
                stopwatch.ElapsedMilliseconds));
        }
        catch (Exception exception)
        {
            requiresQuarantine = RequiresQuarantine(connection, exception);
            if (requiresQuarantine)
            {
                quarantineWorker?.Invoke();
            }

            stopwatch.Stop();
            return Result<QueryResultSet>.Success(new QueryResultSet(
                false,
                exception.Message,
                [],
                [],
                0,
                stopwatch.ElapsedMilliseconds));
        }
        finally
        {
            if (!requiresQuarantine)
            {
                try
                {
                    using var rollbackTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    await using var rollback = connection.CreateCommand();
                    rollback.CommandText = "ROLLBACK";
                    rollback.CommandTimeout = 5;
                    await rollback.ExecuteNonQueryAsync(rollbackTimeout.Token);
                }
                catch
                {
                    quarantineWorker?.Invoke();
                    // Ошибка отката не меняет уже сформированный результат запроса.
                }
            }
        }
    }

    internal static async Task<Result<InspectedSchema>> InspectCatalogAsync(
        DbConnection connection,
        string systemName,
        CancellationToken ct,
        Action? quarantineWorker = null)
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
                tableColumns.Select(pair => new InspectedTable(pair.Key, pair.Value)).ToList(),
                relationships));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            quarantineWorker?.Invoke();
            throw;
        }
        catch (Exception exception)
        {
            if (RequiresQuarantine(connection, exception))
            {
                quarantineWorker?.Invoke();
            }

            return Result<InspectedSchema>.Fail(SandboxErrors.SetupFailed(exception.Message));
        }
    }

    private static bool RequiresQuarantine(DbConnection connection, Exception exception) =>
        connection.State is ConnectionState.Broken or ConnectionState.Closed ||
        exception is OperationCanceledException ||
        exception is MySqlException { ErrorCode: MySqlErrorCode.CommandTimeoutExpired } ||
        exception is PostgresException { SqlState: "57014" } ||
        ContainsTimeout(exception);

    private static CancellationTokenRegistration CancelCommandOnCancellation(
        DbCommand command,
        CancellationToken cancellationToken) => cancellationToken.Register(
        static state =>
        {
            try
            {
                ((DbCommand)state!).Cancel();
            }
            catch
            {
                // Соединение всё равно будет выбраковано вызывающим кодом.
            }
        },
        command);

    private static bool ContainsTimeout(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException!)
        {
            if (current is TimeoutException)
            {
                return true;
            }

            if (current.InnerException is null)
            {
                break;
            }
        }

        return false;
    }
}
