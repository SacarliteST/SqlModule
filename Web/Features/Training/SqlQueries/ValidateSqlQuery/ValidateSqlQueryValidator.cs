using FluentValidation;
using SQLModule.Contracts.Training.SqlQuery;

namespace SQLModule.Web.Features.Training.SqlQueries;

internal sealed class ValidateSqlQueryValidator : AbstractValidator<ValidateSqlQueryRequest>
{
    public ValidateSqlQueryValidator()
    {
        RuleFor(x => x.TargetDbId).NotEmpty();
        RuleFor(x => x.QueryText).NotEmpty();
    }
}
