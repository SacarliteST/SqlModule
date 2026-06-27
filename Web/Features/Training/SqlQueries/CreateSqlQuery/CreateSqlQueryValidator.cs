using FluentValidation;
using SQLModule.Contracts.Training.SqlQuery;

namespace SQLModule.Web.Features.Training.SqlQueries;

internal sealed class CreateSqlQueryValidator : AbstractValidator<CreateSqlQueryRequest>
{
    public CreateSqlQueryValidator()
    {
        RuleFor(x => x.TargetDbId).NotEmpty();
        RuleFor(x => x.QueryText).NotEmpty();
    }
}
