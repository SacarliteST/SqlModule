using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Persistence;
using SQLModule.Application.SpdIntegration;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Skill;

namespace SQLModule.Application.Training.Skills;

public sealed record RestoreSkillCommand(Guid Id) : IRequest<Result<SkillResponse>>;

public sealed class RestoreSkillHandler(IApplicationDbContext db, SkillCatalogOutbox outbox)
    : IRequestHandler<RestoreSkillCommand, Result<SkillResponse>>
{
    public async Task<Result<SkillResponse>> Handle(RestoreSkillCommand command, CancellationToken ct)
    {
        var skill = await db.Skills.FirstOrDefaultAsync(value => value.Id == command.Id, ct);
        if (skill is null)
        {
            return Result<SkillResponse>.Fail(SkillErrors.NotFound(command.Id));
        }

        if (!skill.IsDeleted)
        {
            return Result<SkillResponse>.Fail(SkillErrors.NotDeleted(command.Id));
        }

        skill.Restore();
        outbox.Add(skill);
        await db.SaveChangesAsync(ct);
        return SkillMappings.ToResponse(skill);
    }
}
