using FluentValidation;
using SQLModule.Contracts.Training.SqlQuery;

namespace SQLModule.Host.Features.Training.SqlQueries;

internal sealed class UpdateSqlQueryValidator : AbstractValidator<UpdateSqlQueryRequest>
{
    public UpdateSqlQueryValidator()
    {
        RuleFor(x => x.QueryText).NotEmpty();
    }
}
