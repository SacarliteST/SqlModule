using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Data.Core;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.Schema.DataRecords;

internal record DeleteDataRecordCommand(Guid Id) : IRequest<Result>;

internal sealed class DeleteDataRecordHandler(AppDbContext db)
    : IRequestHandler<DeleteDataRecordCommand, Result>
{
    public async Task<Result> Handle(DeleteDataRecordCommand command, CancellationToken ct)
    {
        var entity = await db.DataRecords.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (entity is null)
        {
            return Result.Fail(DataRecordErrors.NotFound(command.Id));
        }

        db.DataRecords.Remove(entity);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
