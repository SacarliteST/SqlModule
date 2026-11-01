using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Persistence;
using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.DataRecord;
using SQLModule.Domain.Schema;

namespace SQLModule.Application.Schema.DataRecords;

public record CreateDataRecordCommand(Guid MetaTableId, int? SortOrder)
    : IRequest<Result<DataRecordResponse>>;

public sealed class CreateDataRecordHandler(IApplicationDbContext db)
    : IRequestHandler<CreateDataRecordCommand, Result<DataRecordResponse>>
{
    public async Task<Result<DataRecordResponse>> Handle(
        CreateDataRecordCommand command, CancellationToken ct)
    {
        if (!await db.MetaTables.AnyAsync(x => x.Id == command.MetaTableId, ct))
        {
            return Result<DataRecordResponse>.Fail(
                DataRecordErrors.MetaTableNotFound(command.MetaTableId));
        }

        var entity = DataRecord.Create(command.MetaTableId, command.SortOrder);
        db.DataRecords.Add(entity);
        await db.SaveChangesAsync(ct);
        return DataRecordMappings.ToResponse(entity);
    }
}
