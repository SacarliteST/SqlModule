using System.Data.Common;

namespace SQLModule.Sandbox.Dialects;

internal interface ISqlDialect
{
    string SystemName { get; }
    DbConnection CreateConnection(string connectionString);
    string BuildConnectionString(string host, int port, string db, string user, string pwd);
    Task BeginReadOnlyAsync(DbConnection conn, CancellationToken ct);
}
