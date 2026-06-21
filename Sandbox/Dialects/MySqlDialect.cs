using System.Data.Common;
using MySqlConnector;

namespace SQLModule.Sandbox.Dialects;

internal sealed class MySqlDialect : ISqlDialect
{
    public string SystemName => "mysql";

    public DbConnection CreateConnection(string connectionString) => new MySqlConnection(connectionString);

    public string BuildConnectionString(string host, int port, string db, string user, string pwd) =>
        $"Server={host};Port={port};Database={db};User={user};Password={pwd};ConnectionTimeout=10;DefaultCommandTimeout=10";

    public async Task BeginReadOnlyAsync(DbConnection conn, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "START TRANSACTION READ ONLY";
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
