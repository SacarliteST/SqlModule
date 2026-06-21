using System.Data.Common;
using SQLModule.Sandbox;

namespace SQLModule.Sandbox.Dialects;

internal interface ISqlDialect : ISqlSyntax
{
    DbConnection CreateConnection(string connectionString);
    string BuildConnectionString(string host, int port, string db, string user, string pwd);
    Task BeginReadOnlyAsync(DbConnection conn, CancellationToken ct);
}
