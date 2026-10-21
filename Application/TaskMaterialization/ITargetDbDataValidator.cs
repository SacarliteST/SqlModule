using SQLModule.Common.Results;

namespace SQLModule.Application.TaskMaterialization;

/// <summary>Проверяет, что текущее состояние учебных данных применимо в реальной СУБД.</summary>
public interface ITargetDbDataValidator
{
    Task<Result> ValidateAsync(Guid targetDbId, CancellationToken ct);
}
