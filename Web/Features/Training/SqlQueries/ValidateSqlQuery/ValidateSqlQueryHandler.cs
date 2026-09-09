using SQLModule.Common.Results;
using SQLModule.Contracts.Training.SqlQuery;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.SqlQueries;

internal sealed record ValidateSqlQueryCommand(Guid TargetDbId, string QueryText)
    : IRequest<Result<ValidateSqlQueryResponse>>;

internal sealed class ValidateSqlQueryHandler(ISqlQueryValidationRunner validationRunner)
    : IRequestHandler<ValidateSqlQueryCommand, Result<ValidateSqlQueryResponse>>
{
    public async Task<Result<ValidateSqlQueryResponse>> Handle(
        ValidateSqlQueryCommand command,
        CancellationToken ct)
    {
        var result = await validationRunner.ValidateAsync(command.TargetDbId, command.QueryText, ct);
        if (!result.IsSuccess)
        {
            return Result<ValidateSqlQueryResponse>.Fail(result.Error!);
        }

        var preview = result.Value!;
        return new ValidateSqlQueryResponse(
            true,
            preview.Columns,
            preview.Rows,
            preview.RowCount,
            preview.DurationMs,
            DateTimeOffset.UtcNow);
    }
}
