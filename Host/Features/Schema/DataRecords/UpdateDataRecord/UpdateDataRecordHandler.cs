using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Data.Core;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.Schema.DataRecords;

internal record UpdateDataRecordCommand(Guid Id, int? SortOrder) : IRequest<Result>;

internal sealed class UpdateDataRecordHandler(AppDbContext db)
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
