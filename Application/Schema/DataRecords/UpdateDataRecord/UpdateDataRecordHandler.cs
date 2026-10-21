using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Application.Persistence;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.Schema.DataRecords;

public record UpdateDataRecordCommand(Guid Id, int? SortOrder) : IRequest<Result>;

public sealed class UpdateDataRecordHandler(IApplicationDbContext db)
    : IRequestHandler<UpdateDataRecordCommand, Result>
{
    public async Task<Result> Handle(UpdateDataRecordCommand command, CancellationToken ct)
    {
        var entity = await db.DataRecords.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (entity is null)
        {
            return Result.Fail(DataRecordErrors.NotFound(command.Id));
        }

        entity.Update(command.SortOrder);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
