using FluentValidation;
using SQLModule.Contracts.Training.SqlQuery;

namespace SQLModule.Web.Features.Training.SqlQueries;

internal sealed class GetAllSqlQueriesValidator : AbstractValidator<GetAllSqlQueriesRequest>
{
    public GetAllSqlQueriesValidator()
    {
        RuleFor(x => x.Offset).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Limit).GreaterThan(0).LessThanOrEqualTo(100);
    }
}
