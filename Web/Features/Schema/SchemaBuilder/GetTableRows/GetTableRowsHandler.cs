using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Data.Core;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.SchemaBuilder.GetTableRows;

internal sealed record GetTableRowsQuery(Guid TargetDbId, Guid TableId, int Offset, int Limit)
    : IRequest<Result<TableRowsResponse>>;

internal sealed class GetTableRowsHandler(AppDbContext db)
    : IRequestHandler<GetTableRowsQuery, Result<TableRowsResponse>>
{
    public async Task<Result<TableRowsResponse>> Handle(GetTableRowsQuery query, CancellationToken ct)
    {
        var target = await db.TargetDbs.AsNoTracking()
            .Where(x => x.Id == query.TargetDbId)
            .Select(x => new { x.SchemaVersion })
            .SingleOrDefaultAsync(ct);
        if (target is null)
        {
            return Result<TableRowsResponse>.Fail(SchemaAggregateErrors.NotFound(query.TargetDbId));
        }

        var tableExists = await db.MetaTables.AsNoTracking()
            .AnyAsync(x => x.Id == query.TableId && x.TargetDbId == query.TargetDbId, ct);
        if (!tableExists)
        {
            return Result<TableRowsResponse>.Fail(Error.NotFound("MetaTable", query.TableId));
        }

        var columns = await db.MetaAttributes.AsNoTracking()
            .Where(x => x.MetaTableId == query.TableId)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .Select(x => new TableRowsColumnResponse
            {
                Id = x.Id,
                Name = x.AttributeName,
                PhysicalTypeName = x.PhysicalType.TypeName,
                IsRequired = x.IsRequired
            }).ToListAsync(ct);
        var count = await db.DataRecords.CountAsync(x => x.MetaTableId == query.TableId, ct);
        var records = await db.DataRecords.AsNoTracking()
            .Where(x => x.MetaTableId == query.TableId)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .Skip(query.Offset).Take(query.Limit)
            .Select(x => new { x.Id, x.SortOrder, x.UpdatedAt })
            .ToListAsync(ct);
        var recordIds = records.Select(x => x.Id).ToList();
        var cells = await db.CellValues.AsNoTracking()
            .Where(x => recordIds.Contains(x.DataRecordId))
            .Select(x => new { x.DataRecordId, x.MetaAttributeId, x.TextValue })
            .ToListAsync(ct);
        var cellsByRecord = cells.ToLookup(x => x.DataRecordId);

        return new TableRowsResponse
        {
            Count = count,
            Offset = query.Offset,
            Limit = query.Limit,
            SchemaVersion = target.SchemaVersion.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Columns = columns,
            Items = records.Select(record =>
            {
                var values = cellsByRecord[record.Id].ToDictionary(x => x.MetaAttributeId, x => x.TextValue);
                return new TableRowResponse
                {
                    Id = record.Id,
                    Version = record.UpdatedAt.ToUnixTimeMilliseconds().ToString(System.Globalization.CultureInfo.InvariantCulture),
                    SortOrder = record.SortOrder ?? 0,
                    Cells = columns.ToDictionary(
                        column => column.Id,
                        column => new TableCellResponse
                        {
                            Value = values.GetValueOrDefault(column.Id),
                            IsNull = !values.TryGetValue(column.Id, out var value) || value is null
                        })
                };
            }).ToList()
        };
    }
}
