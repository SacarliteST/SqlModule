using System.Data.Common;
using Npgsql;

namespace SQLModule.Sandbox.Dialects;

internal sealed class PostgresDialect : ISqlDialect
{
    public string SystemName => "postgres";

    public DbConnection CreateConnection(string connectionString) => new NpgsqlConnection(connectionString);

    public string BuildConnectionString(string host, int port, string db, string user, string pwd) =>
        $"Host={host};Port={port};Database={db};Username={user};Password={pwd};Timeout=10;Command Timeout=10";

    public async Task BeginReadOnlyAsync(DbConnection conn, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "BEGIN READ ONLY";
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
