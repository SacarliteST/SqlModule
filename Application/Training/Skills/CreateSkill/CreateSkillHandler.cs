using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Persistence;
using SQLModule.Application.SpdIntegration;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Skill;
using SQLModule.Domain.Training;

namespace SQLModule.Application.Training.Skills;

public sealed record CreateSkillCommand(string SkillId, string Name, string? Description)
    : IRequest<Result<SkillResponse>>;

public sealed class CreateSkillHandler(IApplicationDbContext db, SkillCatalogOutbox outbox)
    : IRequestHandler<CreateSkillCommand, Result<SkillResponse>>
{
    public async Task<Result<SkillResponse>> Handle(CreateSkillCommand command, CancellationToken ct)
    {
        if (await db.Skills.AsNoTracking().AnyAsync(skill => skill.SkillId == command.SkillId, ct))
        {
            return Result<SkillResponse>.Fail(SkillErrors.AlreadyExists(command.SkillId));
        }

        var skill = Skill.CreateTeacherDefined(command.SkillId, command.Name, command.Description);
        db.Skills.Add(skill);
        outbox.Add(skill);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return Result<SkillResponse>.Fail(SkillErrors.AlreadyExists(command.SkillId));
        }

        return SkillMappings.ToResponse(skill);
    }
}
