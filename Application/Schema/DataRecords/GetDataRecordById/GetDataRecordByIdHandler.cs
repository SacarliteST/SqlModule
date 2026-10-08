using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.DataRecord;
using SQLModule.Application.Persistence;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.Schema.DataRecords;

public record GetDataRecordByIdQuery(Guid Id) : IRequest<Result<DataRecordResponse>>;

public sealed class GetDataRecordByIdHandler(IApplicationDbContext db)
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
