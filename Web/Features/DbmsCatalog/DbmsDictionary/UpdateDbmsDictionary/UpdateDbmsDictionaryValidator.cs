using FluentValidation;
using SQLModule.Contracts.DbmsCatalog.DbmsDictionary;

namespace SQLModule.Web.Features.DbmsCatalog.DbmsDictionary.UpdateDbmsDictionary;

internal sealed class UpdateDbmsDictionaryValidator : AbstractValidator<UpdateDbmsDictionaryRequest>
{
    public UpdateDbmsDictionaryValidator()
    {
        RuleFor(x => x.DbmsName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DbmsSystemName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DockerImage).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DefaultPort).InclusiveBetween(1, 65535);
        RuleFor(x => x.EnvUserKey).NotEmpty().MaximumLength(100);
        RuleFor(x => x.EnvPasswordKey).NotEmpty().MaximumLength(100);
        RuleFor(x => x.EnvDatabaseKey).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ExtraEnvConfig).MaximumLength(500);
        RuleFor(x => x.DefaultDatabase).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DefaultUsername).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DefaultPassword).NotEmpty().MaximumLength(100);
    }
}
