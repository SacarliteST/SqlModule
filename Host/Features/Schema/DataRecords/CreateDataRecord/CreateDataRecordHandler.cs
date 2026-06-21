using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.DataRecord;
using SQLModule.Data.Core;
using SQLModule.Domain.Schema;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.Schema.DataRecords;

internal record CreateDataRecordCommand(Guid MetaTableId, int? SortOrder)
    : IRequest<Result<DataRecordResponse>>;

internal sealed class CreateDataRecordHandler(AppDbContext db)
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
