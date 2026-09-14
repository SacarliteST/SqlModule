using SQLModule.Common.Results;

namespace SQLModule.Web.Common.Sandbox;

/// <summary>Проверяет, что текущее состояние учебных данных применимо в реальной СУБД.</summary>
internal interface ITargetDbDataValidator
{
    Task<Result> ValidateAsync(Guid targetDbId, CancellationToken ct);
}
