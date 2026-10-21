using SQLModule.Application.DbmsCatalog.DbmsDictionary;
using SQLModule.Application.DbmsCatalog.DbmsDictionary.GetAllDbmsDictionaries;
﻿using FluentValidation;
using SQLModule.Contracts.DbmsCatalog.DbmsDictionary;

namespace SQLModule.Web.Features.DbmsCatalog.DbmsDictionary.GetAllDbmsDictionaries;

internal sealed class GetAllDbmsDictionariesValidator : AbstractValidator<GetAllDbmsDictionariesRequest>
{
    public GetAllDbmsDictionariesValidator()
    {
        RuleFor(x => x.Offset).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Limit).GreaterThan(0).LessThanOrEqualTo(100);
    }
}
