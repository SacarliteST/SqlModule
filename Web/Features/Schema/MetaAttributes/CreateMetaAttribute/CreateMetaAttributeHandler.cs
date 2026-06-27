using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.MetaAttribute;
using SQLModule.Data.Core;
using SQLModule.Domain.Schema;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.MetaAttributes;

internal record CreateMetaAttributeCommand(
    Guid MetaTableId,
    Guid PhysicalTypeId,
    string AttributeName,
    bool IsPrimaryKey,
    bool IsRequired,
    short SortOrder) : IRequest<Result<MetaAttributeResponse>>;

internal sealed class CreateMetaAttributeHandler(AppDbContext db)
    : IRequestHandler<CreateMetaAttributeCommand, Result<MetaAttributeResponse>>
{
    public async Task<Result<MetaAttributeResponse>> Handle(
        CreateMetaAttributeCommand command, CancellationToken ct)
    {
        if (!await db.MetaTables.AnyAsync(x => x.Id == command.MetaTableId, ct))
        {
            return Result<MetaAttributeResponse>.Fail(
                MetaAttributeErrors.MetaTableNotFound(command.MetaTableId));
        }

        if (!await db.PhysicalTypes.AnyAsync(x => x.Id == command.PhysicalTypeId, ct))
        {
            return Result<MetaAttributeResponse>.Fail(
                MetaAttributeErrors.PhysicalTypeNotFound(command.PhysicalTypeId));
        }

        var entity = MetaAttribute.Create(
            command.MetaTableId, command.PhysicalTypeId, command.AttributeName,
            command.IsPrimaryKey, command.IsRequired, command.SortOrder);

        db.MetaAttributes.Add(entity);
        await db.SaveChangesAsync(ct);
        return MetaAttributeMappings.ToResponse(entity);
    }
}
