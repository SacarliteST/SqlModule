using System.Data.Common;
using SQLModule.Sandbox;
using SQLModule.Sandbox.Pooling;

namespace SQLModule.Sandbox.Dialects;

internal interface ISqlDialect : ISqlSyntax
{
    DbConnection CreateConnection(string connectionString);
    string BuildConnectionString(string host, int port, string db, string user, string pwd);
    string BuildControlConnectionString(string host, int port, SandboxDbmsSpec dbms);
    Task BeginReadOnlyAsync(DbConnection conn, CancellationToken ct);
    Task CreateIsolationNamespaceAsync(
        DbConnection controlConnection,
        SandboxIsolationNamespace sandboxNamespace,
        CancellationToken ct);
    Task GrantRunnerAccessAsync(
        DbConnection setupConnection,
        SandboxIsolationNamespace sandboxNamespace,
        CancellationToken ct);
    Task CleanupIsolationNamespaceAsync(
        DbConnection controlConnection,
        SandboxIsolationNamespace sandboxNamespace,
        CancellationToken ct);
    Task<bool> IsolationNamespaceExistsAsync(
        DbConnection controlConnection,
        SandboxIsolationNamespace sandboxNamespace,
        CancellationToken ct);
}
