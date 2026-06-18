using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace SQLModule.Host.Features.Schema.TargetDbs;

internal record GetAllTargetDbsRequest(
    [FromQuery] int Offset = 0,
    [FromQuery] int Limit = 20);

internal sealed class GetAllTargetDbsValidator : AbstractValidator<GetAllTargetDbsRequest>
{
    public GetAllTargetDbsValidator()
    {
        RuleFor(x => x.Offset).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Limit).GreaterThan(0).LessThanOrEqualTo(100);
    }
}
