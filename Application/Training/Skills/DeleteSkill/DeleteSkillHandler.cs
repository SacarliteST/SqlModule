using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Persistence;
using SQLModule.Application.SpdIntegration;
using SQLModule.Common.Results;

namespace SQLModule.Application.Training.Skills;

public sealed record DeleteSkillCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteSkillHandler(IApplicationDbContext db, SkillCatalogOutbox outbox)
    : IRequestHandler<DeleteSkillCommand, Result>
{
    public async Task<Result> Handle(DeleteSkillCommand command, CancellationToken ct)
    {
        var skill = await db.Skills.FirstOrDefaultAsync(value => value.Id == command.Id, ct);
        if (skill is null)
        {
            return Result.Fail(SkillErrors.NotFound(command.Id));
        }

        if (skill.IsDeleted)
        {
            return Result.Fail(SkillErrors.AlreadyDeleted(command.Id));
        }

        skill.Delete();
        outbox.Add(skill);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
