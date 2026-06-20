using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts.DbmsCatalog.PhysicalType;
using SQLModule.Data.Core;
using SQLModule.Domain.DbmsCatalog;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.DbmsCatalog.PhysicalTypes;

internal record CreatePhysicalTypeCommand(
    Guid DbmsId,
    string TypeName) : IRequest<Result<PhysicalTypeResponse>>;

internal sealed class CreatePhysicalTypeHandler(AppDbContext db)
    : IRequestHandler<CreatePhysicalTypeCommand, Result<PhysicalTypeResponse>>
{
    public async Task<Result<PhysicalTypeResponse>> Handle(
        CreatePhysicalTypeCommand command, CancellationToken ct)
    {
        if (!await db.DbmsDictionaries.AnyAsync(x => x.Id == command.DbmsId, ct))
        {
            return Result<PhysicalTypeResponse>.Fail(
                PhysicalTypeErrors.DbmsNotFound(command.DbmsId));
        }

        var entity = PhysicalType.Create(command.DbmsId, command.TypeName);
        db.PhysicalTypes.Add(entity);
        await db.SaveChangesAsync(ct);
        return PhysicalTypeMappings.ToResponse(entity);
    }
}
