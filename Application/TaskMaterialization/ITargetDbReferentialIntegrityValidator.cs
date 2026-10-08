using SQLModule.Common.Results;

namespace SQLModule.Application.TaskMaterialization;

public interface ITargetDbReferentialIntegrityValidator
{
    Task<Result> ValidateAsync(Guid targetDbId, CancellationToken ct);
}
