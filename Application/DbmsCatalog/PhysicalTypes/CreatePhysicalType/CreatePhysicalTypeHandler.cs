using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.DbmsCatalog.PhysicalType;
using SQLModule.Application.Persistence;
using SQLModule.Domain.DbmsCatalog;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.DbmsCatalog.PhysicalTypes;

public record CreatePhysicalTypeCommand(
    Guid DbmsId,
    string TypeName) : IRequest<Result<PhysicalTypeResponse>>;

public sealed class CreatePhysicalTypeHandler(IApplicationDbContext db)
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
