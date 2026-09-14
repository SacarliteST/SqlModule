using SQLModule.Common.Results;

namespace SQLModule.Web.Common.Sandbox;

internal interface ITargetDbReferentialIntegrityValidator
{
    Task<Result> ValidateAsync(Guid targetDbId, CancellationToken ct);
}
