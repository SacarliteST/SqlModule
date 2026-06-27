using FluentValidation;
using SQLModule.Contracts.DbmsCatalog.ParameterDefinition;

namespace SQLModule.Web.Features.DbmsCatalog.ParameterDefinitions;

internal sealed class GetAllParameterDefinitionsValidator : AbstractValidator<GetAllParameterDefinitionsRequest>
{
    public GetAllParameterDefinitionsValidator()
    {
        RuleFor(x => x.Offset).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Limit).GreaterThan(0).LessThanOrEqualTo(100);
    }
}
