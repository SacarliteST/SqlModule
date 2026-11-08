using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Persistence;
using SQLModule.Application.SpdIntegration;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Skill;

namespace SQLModule.Application.Training.Skills;

public sealed record UpdateSkillCommand(Guid Id, string Name, string? Description)
    : IRequest<Result<SkillResponse>>;

public sealed class UpdateSkillHandler(IApplicationDbContext db, SkillCatalogOutbox outbox)
    : IRequestHandler<UpdateSkillCommand, Result<SkillResponse>>
{
    public async Task<Result<SkillResponse>> Handle(UpdateSkillCommand command, CancellationToken ct)
    {
        var skill = await db.Skills.FirstOrDefaultAsync(value => value.Id == command.Id, ct);
        if (skill is null)
        {
            return Result<SkillResponse>.Fail(SkillErrors.NotFound(command.Id));
        }

        skill.Update(command.Name, command.Description);
        outbox.Add(skill);
        await db.SaveChangesAsync(ct);
        return SkillMappings.ToResponse(skill);
    }
}
