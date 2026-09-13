using System.Data.Common;
using System.Globalization;
using Npgsql;
using SQLModule.Sandbox.Pooling;

namespace SQLModule.Sandbox.Dialects;

internal sealed class PostgresDialect : ISqlDialect
{
    public string SystemName => "postgres";

    public string QuoteIdentifier(string name) => $"\"{name.Replace("\"", "\"\"")}\"";

    public string FormatValue(string? value)
    {
        if (value is null)
        {
            return "NULL";
        }

        if (Decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
        {
            return value;
        }

        return $"'{value.Replace("'", "''")}'";
    }

    public DbConnection CreateConnection(string connectionString) => new NpgsqlConnection(connectionString);

    public string BuildConnectionString(string host, int port, string db, string user, string pwd) =>
        $"Host={host};Port={port};Database={db};Username={user};Password={pwd};Timeout=10;Command Timeout=10";

    public string BuildControlConnectionString(string host, int port, SandboxDbmsSpec dbms) =>
        BuildConnectionString(host, port, dbms.DefaultDatabase, dbms.DefaultUsername, dbms.DefaultPassword);

    public async Task BeginReadOnlyAsync(DbConnection conn, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "BEGIN READ ONLY";
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task CreateIsolationNamespaceAsync(
        DbConnection controlConnection,
        SandboxIsolationNamespace sandboxNamespace,
        CancellationToken ct)
    {
        await ExecuteAsync(
            controlConnection,
            $"CREATE ROLE {QuoteIdentifier(sandboxNamespace.SetupUsername)} LOGIN PASSWORD {QuoteGeneratedPassword(sandboxNamespace.SetupPassword)} NOSUPERUSER NOCREATEDB NOCREATEROLE INHERIT",
            ct);
        await ExecuteAsync(
            controlConnection,
            $"CREATE ROLE {QuoteIdentifier(sandboxNamespace.RunnerUsername)} LOGIN PASSWORD {QuoteGeneratedPassword(sandboxNamespace.RunnerPassword)} NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT",
            ct);
        await ExecuteAsync(
            controlConnection,
            $"CREATE DATABASE {QuoteIdentifier(sandboxNamespace.DatabaseName)} OWNER {QuoteIdentifier(sandboxNamespace.SetupUsername)}",
            ct);
        await ExecuteAsync(
            controlConnection,
            $"REVOKE ALL ON DATABASE {QuoteIdentifier(sandboxNamespace.DatabaseName)} FROM PUBLIC",
            ct);
        await ExecuteAsync(
            controlConnection,
            $"GRANT CONNECT ON DATABASE {QuoteIdentifier(sandboxNamespace.DatabaseName)} TO {QuoteIdentifier(sandboxNamespace.RunnerUsername)}",
            ct);
    }

    public async Task GrantRunnerAccessAsync(
        DbConnection setupConnection,
        SandboxIsolationNamespace sandboxNamespace,
        CancellationToken ct)
    {
        await ExecuteAsync(setupConnection, "REVOKE CREATE ON SCHEMA public FROM PUBLIC", ct);
        await ExecuteAsync(
            setupConnection,
            $"GRANT USAGE ON SCHEMA public TO {QuoteIdentifier(sandboxNamespace.RunnerUsername)}",
            ct);
        await ExecuteAsync(
            setupConnection,
            $"GRANT SELECT ON ALL TABLES IN SCHEMA public TO {QuoteIdentifier(sandboxNamespace.RunnerUsername)}",
            ct);
        await ExecuteAsync(
            setupConnection,
            $"GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO {QuoteIdentifier(sandboxNamespace.RunnerUsername)}",
            ct);
    }

    public async Task CleanupIsolationNamespaceAsync(
        DbConnection controlConnection,
        SandboxIsolationNamespace sandboxNamespace,
        CancellationToken ct)
    {
        await ExecuteAsync(
            controlConnection,
            "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @database AND pid <> pg_backend_pid()",
            ct,
            ("database", sandboxNamespace.DatabaseName));
        await ExecuteAsync(
            controlConnection,
            $"DROP DATABASE IF EXISTS {QuoteIdentifier(sandboxNamespace.DatabaseName)} WITH (FORCE)",
            ct);
        await ExecuteAsync(
            controlConnection,
            $"DROP ROLE IF EXISTS {QuoteIdentifier(sandboxNamespace.RunnerUsername)}",
            ct);
        await ExecuteAsync(
            controlConnection,
            $"DROP ROLE IF EXISTS {QuoteIdentifier(sandboxNamespace.SetupUsername)}",
            ct);
    }

    public async Task<bool> IsolationNamespaceExistsAsync(
        DbConnection controlConnection,
        SandboxIsolationNamespace sandboxNamespace,
        CancellationToken ct)
    {
        await using var command = controlConnection.CreateCommand();
        command.CommandText = "SELECT EXISTS (SELECT FROM pg_database WHERE datname = @database) OR " +
                              "EXISTS (SELECT FROM pg_roles WHERE rolname IN (@setup, @runner))";
        AddParameter(command, "database", sandboxNamespace.DatabaseName);
        AddParameter(command, "setup", sandboxNamespace.SetupUsername);
        AddParameter(command, "runner", sandboxNamespace.RunnerUsername);
        return Convert.ToBoolean(await command.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture);
    }

    private static async Task ExecuteAsync(
        DbConnection connection,
        string sql,
        CancellationToken ct,
        params (string Name, object Value)[] parameters)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var parameter in parameters)
        {
            AddParameter(command, parameter.Name, parameter.Value);
        }

        await command.ExecuteNonQueryAsync(ct);
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static string QuoteGeneratedPassword(string password)
    {
        if (password.Length != 64 || password.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new InvalidOperationException("Sandbox password has an invalid format.");
        }

        return $"'{password}'";
    }
}
