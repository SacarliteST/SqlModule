using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.DataRecord;
using SQLModule.Data.Core;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.Schema.DataRecords;

internal record GetAllDataRecordsQuery(int Offset, int Limit, Guid? MetaTableId)
    : IRequest<Result<PageResponse<DataRecordResponse>>>;

internal sealed class GetAllDataRecordsHandler(AppDbContext db)
    : IRequestHandler<GetAllDataRecordsQuery, Result<PageResponse<DataRecordResponse>>>
{
    public async Task<Result<PageResponse<DataRecordResponse>>> Handle(
        GetAllDataRecordsQuery query, CancellationToken ct)
    {
        var source = db.DataRecords.AsNoTracking();

        if (query.MetaTableId.HasValue)
        {
            source = source.Where(r => r.MetaTableId == query.MetaTableId.Value);
        }

        var total = await source.CountAsync(ct);
        var entities = await source
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.Id)
            .Skip(query.Offset)
            .Take(query.Limit)
            .ToListAsync(ct);

        return Result<PageResponse<DataRecordResponse>>.Success(new PageResponse<DataRecordResponse>
        {
            Items = entities.Select(DataRecordMappings.ToResponse).ToList(),
            Count = total
        });
    }
}
