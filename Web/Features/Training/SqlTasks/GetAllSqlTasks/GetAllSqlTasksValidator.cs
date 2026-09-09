using FluentValidation;
using SQLModule.Contracts.Training.SqlTask;

namespace SQLModule.Web.Features.Training.SqlTasks;

internal sealed class GetAllSqlTasksValidator : AbstractValidator<GetAllSqlTasksRequest>
{
    public GetAllSqlTasksValidator()
    {
        RuleFor(x => x.Offset).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Limit).GreaterThan(0).LessThanOrEqualTo(100);
        RuleFor(x => x.DifficultyLevel).InclusiveBetween((short)1, (short)5)
            .When(x => x.DifficultyLevel.HasValue);
        RuleFor(x => x.PublicationStatus).IsInEnum().When(x => x.PublicationStatus.HasValue);
    }
}
