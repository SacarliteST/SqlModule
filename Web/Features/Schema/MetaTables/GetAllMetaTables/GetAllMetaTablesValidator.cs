using FluentValidation;
using SQLModule.Contracts.Schema.MetaTable;

namespace SQLModule.Web.Features.Schema.MetaTables;

internal sealed class GetAllMetaTablesValidator : AbstractValidator<GetAllMetaTablesRequest>
{
    public GetAllMetaTablesValidator()
    {
        RuleFor(x => x.Offset).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Limit).GreaterThan(0).LessThanOrEqualTo(100);
    }
}
