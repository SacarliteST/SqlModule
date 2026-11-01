using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Persistence;
using SQLModule.Common.Results;

namespace SQLModule.Application.Schema.MetaAttributes;

public record UpdateMetaAttributeCommand(
    Guid Id,
    string AttributeName,
    bool IsPrimaryKey,
    bool IsRequired,
    short SortOrder) : IRequest<Result>;

public sealed class UpdateMetaAttributeHandler(IApplicationDbContext db)
    : IRequestHandler<UpdateMetaAttributeCommand, Result>
{
    public async Task<Result> Handle(UpdateMetaAttributeCommand command, CancellationToken ct)
    {
        var entity = await db.MetaAttributes.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (entity is null)
        {
            return Result.Fail(MetaAttributeErrors.NotFound(command.Id));
        }

        entity.Update(command.AttributeName, command.IsPrimaryKey, command.IsRequired, command.SortOrder);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
