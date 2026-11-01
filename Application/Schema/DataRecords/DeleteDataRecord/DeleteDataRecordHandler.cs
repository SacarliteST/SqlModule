using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Persistence;
using SQLModule.Common.Results;

namespace SQLModule.Application.Schema.DataRecords;

public record DeleteDataRecordCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteDataRecordHandler(IApplicationDbContext db)
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
