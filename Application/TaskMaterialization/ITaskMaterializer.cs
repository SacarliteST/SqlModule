using SQLModule.Common.Results;
using SQLModule.Domain.DbmsCatalog;
using SQLModule.Sandbox;

namespace SQLModule.Application.TaskMaterialization;

/// <summary>Собирает DDL + INSERT для TargetDb в <see cref="SandboxSetup"/>.</summary>
public sealed record MaterializedTask(DbmsDictionary Dbms, SandboxSetup Setup);

public interface ITaskMaterializer
{
    Task<Result<MaterializedTask>> MaterializeAsync(Guid targetDbId, CancellationToken ct);
}
