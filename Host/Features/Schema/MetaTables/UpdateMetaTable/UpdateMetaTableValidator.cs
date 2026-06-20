using FluentValidation;
using SQLModule.Contracts.Schema.MetaTable;

namespace SQLModule.Host.Features.Schema.MetaTables;

internal sealed class UpdateMetaTableValidator : AbstractValidator<UpdateMetaTableRequest>
{
    public UpdateMetaTableValidator()
    {
        RuleFor(x => x.TableName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}
