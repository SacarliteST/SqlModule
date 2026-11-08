using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Persistence;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Skill;

namespace SQLModule.Application.Training.Skills;

public sealed record GetSkillByIdQuery(Guid Id) : IRequest<Result<SkillResponse>>;

public sealed class GetSkillByIdHandler(IApplicationDbContext db)
    : IRequestHandler<GetSkillByIdQuery, Result<SkillResponse>>
{
    public async Task<Result<SkillResponse>> Handle(GetSkillByIdQuery query, CancellationToken ct)
    {
        var skill = await db.Skills.AsNoTracking().FirstOrDefaultAsync(value => value.Id == query.Id, ct);
        return skill is null
            ? Result<SkillResponse>.Fail(SkillErrors.NotFound(query.Id))
            : SkillMappings.ToResponse(skill);
    }
}
