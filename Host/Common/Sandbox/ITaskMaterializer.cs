using SQLModule.Common.Results;
using SQLModule.Domain.DbmsCatalog;
using SQLModule.Sandbox;

namespace SQLModule.Host.Common.Sandbox;

/// <summary>Собирает DDL + INSERT для TargetDb в <see cref="SandboxSetup"/>.</summary>
internal sealed record MaterializedTask(DbmsDictionary Dbms, SandboxSetup Setup);

internal interface ITaskMaterializer
{
    Task<Result<MaterializedTask>> MaterializeAsync(Guid targetDbId, CancellationToken ct);
}
