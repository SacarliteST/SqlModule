using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Data.Core;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.MetaAttributes;

internal record UpdateMetaAttributeCommand(
    Guid Id,
    string AttributeName,
    bool IsPrimaryKey,
    bool IsRequired,
    short SortOrder) : IRequest<Result>;

internal sealed class UpdateMetaAttributeHandler(AppDbContext db)
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
