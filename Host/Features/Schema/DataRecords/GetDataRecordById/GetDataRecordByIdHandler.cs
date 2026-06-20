using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts.Schema.DataRecord;
using SQLModule.Data.Core;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Schema.DataRecords;

internal record GetDataRecordByIdQuery(Guid Id) : IRequest<Result<DataRecordResponse>>;

internal sealed class GetDataRecordByIdHandler(AppDbContext db)
    : IRequestHandler<GetDataRecordByIdQuery, Result<DataRecordResponse>>
{
    public async Task<Result<DataRecordResponse>> Handle(
        GetDataRecordByIdQuery query, CancellationToken ct)
    {
        var entity = await db.DataRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.Id, ct);

        if (entity is null)
        {
            return Result<DataRecordResponse>.Fail(DataRecordErrors.NotFound(query.Id));
        }

        return DataRecordMappings.ToResponse(entity);
    }
}
