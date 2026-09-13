using System.Data.Common;
using System.Globalization;
using MySqlConnector;
using SQLModule.Sandbox.Pooling;

namespace SQLModule.Sandbox.Dialects;

internal sealed class MySqlDialect : ISqlDialect
{
    public string SystemName => "mysql";

    public string QuoteIdentifier(string name) => $"`{name.Replace("`", "``")}`";

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

    public DbConnection CreateConnection(string connectionString) => new MySqlConnection(connectionString);

    public string BuildConnectionString(string host, int port, string db, string user, string pwd) =>
        $"Server={host};Port={port};Database={db};User={user};Password={pwd};ConnectionTimeout=10;DefaultCommandTimeout=10";

    public string BuildControlConnectionString(string host, int port, SandboxDbmsSpec dbms)
    {
        if (dbms.DefaultUsername.Equals("root", StringComparison.OrdinalIgnoreCase))
        {
            return BuildConnectionString(host, port, dbms.DefaultDatabase, "root", dbms.DefaultPassword);
        }

        var environment = ParseEnvironment(dbms.ExtraEnvConfig);
        var rootPassword = environment.GetValueOrDefault("MYSQL_ROOT_PASSWORD") ??
                           environment.GetValueOrDefault("MARIADB_ROOT_PASSWORD") ??
                           throw new InvalidOperationException(
                               "Для control-admin MySQL/MariaDB не настроен root password.");
        return BuildConnectionString(host, port, dbms.DefaultDatabase, "root", rootPassword);
    }

    public async Task BeginReadOnlyAsync(DbConnection conn, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "START TRANSACTION READ ONLY";
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task CreateIsolationNamespaceAsync(
        DbConnection controlConnection,
        SandboxIsolationNamespace sandboxNamespace,
        CancellationToken ct)
    {
        await ExecuteAsync(
            controlConnection,
            $"CREATE DATABASE {QuoteIdentifier(sandboxNamespace.DatabaseName)}",
            ct);
        await ExecuteAsync(
            controlConnection,
            $"CREATE USER {QuoteAccount(sandboxNamespace.SetupUsername)} IDENTIFIED BY {QuoteGeneratedPassword(sandboxNamespace.SetupPassword)}",
            ct);
        await ExecuteAsync(
            controlConnection,
            $"CREATE USER {QuoteAccount(sandboxNamespace.RunnerUsername)} IDENTIFIED BY {QuoteGeneratedPassword(sandboxNamespace.RunnerPassword)}",
            ct);
        await ExecuteAsync(
            controlConnection,
            $"GRANT CREATE, ALTER, DROP, INDEX, REFERENCES, SELECT, INSERT, UPDATE, DELETE, " +
            $"CREATE VIEW, SHOW VIEW, TRIGGER, CREATE TEMPORARY TABLES ON " +
            $"{QuoteIdentifier(sandboxNamespace.DatabaseName)}.* TO {QuoteAccount(sandboxNamespace.SetupUsername)}",
            ct);
        await ExecuteAsync(
            controlConnection,
            $"GRANT SELECT ON {QuoteIdentifier(sandboxNamespace.DatabaseName)}.* TO {QuoteAccount(sandboxNamespace.RunnerUsername)}",
            ct);
    }

    public Task GrantRunnerAccessAsync(
        DbConnection setupConnection,
        SandboxIsolationNamespace sandboxNamespace,
        CancellationToken ct) => Task.CompletedTask;

    public async Task CleanupIsolationNamespaceAsync(
        DbConnection controlConnection,
        SandboxIsolationNamespace sandboxNamespace,
        CancellationToken ct)
    {
        var connectionIds = new List<long>();
        await using (var command = controlConnection.CreateCommand())
        {
            command.CommandText =
                "SELECT ID FROM information_schema.processlist WHERE USER IN (@setup, @runner) AND ID <> CONNECTION_ID()";
            AddParameter(command, "setup", sandboxNamespace.SetupUsername);
            AddParameter(command, "runner", sandboxNamespace.RunnerUsername);
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                connectionIds.Add(reader.GetInt64(0));
            }
        }

        foreach (var connectionId in connectionIds)
        {
            await ExecuteAsync(controlConnection, $"KILL CONNECTION {connectionId}", ct);
        }

        await ExecuteAsync(
            controlConnection,
            $"DROP DATABASE IF EXISTS {QuoteIdentifier(sandboxNamespace.DatabaseName)}",
            ct);
        await ExecuteAsync(
            controlConnection,
            $"DROP USER IF EXISTS {QuoteAccount(sandboxNamespace.RunnerUsername)}",
            ct);
        await ExecuteAsync(
            controlConnection,
            $"DROP USER IF EXISTS {QuoteAccount(sandboxNamespace.SetupUsername)}",
            ct);
    }

    public async Task<bool> IsolationNamespaceExistsAsync(
        DbConnection controlConnection,
        SandboxIsolationNamespace sandboxNamespace,
        CancellationToken ct)
    {
        await using var command = controlConnection.CreateCommand();
        command.CommandText = "SELECT EXISTS (SELECT 1 FROM information_schema.schemata WHERE schema_name = @database) OR " +
                              "EXISTS (SELECT 1 FROM mysql.user WHERE User IN (@setup, @runner))";
        AddParameter(command, "database", sandboxNamespace.DatabaseName);
        AddParameter(command, "setup", sandboxNamespace.SetupUsername);
        AddParameter(command, "runner", sandboxNamespace.RunnerUsername);
        return Convert.ToBoolean(await command.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture);
    }

    private static string QuoteAccount(string username) => $"'{username}'@'%'";

    private static string QuoteGeneratedPassword(string password)
    {
        if (password.Length != 64 || password.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new InvalidOperationException("Sandbox password has an invalid format.");
        }

        return $"'{password}'";
    }

    private static IReadOnlyDictionary<string, string> ParseEnvironment(string? extraEnvironment)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (String.IsNullOrWhiteSpace(extraEnvironment))
        {
            return result;
        }

        foreach (var pair in extraEnvironment.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = pair.IndexOf('=');
            if (separator > 0)
            {
                result[pair[..separator].Trim()] = pair[(separator + 1)..].Trim();
            }
        }

        return result;
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
}
