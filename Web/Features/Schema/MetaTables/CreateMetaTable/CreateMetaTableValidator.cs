using FluentValidation;
using SQLModule.Contracts.Schema.MetaTable;

namespace SQLModule.Web.Features.Schema.MetaTables;

internal sealed class CreateMetaTableValidator : AbstractValidator<CreateMetaTableRequest>
{
    public CreateMetaTableValidator()
    {
        RuleFor(x => x.TargetDbId).NotEmpty();
        RuleFor(x => x.TableName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}
