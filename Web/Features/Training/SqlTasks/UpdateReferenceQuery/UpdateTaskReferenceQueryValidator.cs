using FluentValidation;
using SQLModule.Contracts.Training.SqlTask;

namespace SQLModule.Web.Features.Training.SqlTasks;

internal sealed class UpdateTaskReferenceQueryValidator : AbstractValidator<UpdateTaskReferenceQueryRequest>
{
    public UpdateTaskReferenceQueryValidator()
    {
        RuleFor(x => x.TargetDbId).NotNull().NotEmpty();
        RuleFor(x => x.QueryText).NotEmpty();
        RuleFor(x => x.StrictColumnOrder).NotNull();
        RuleFor(x => x.StrictRowOrder).NotNull();
    }
}
