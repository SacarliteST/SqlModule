using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.CellValue;
using SQLModule.Data.Core;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.Schema.CellValues;

internal record GetAllCellValuesQuery(int Offset, int Limit)
    : IRequest<Result<PageResponse<CellValueResponse>>>;

internal sealed class GetAllCellValuesHandler(AppDbContext db)
    : IRequestHandler<GetAllCellValuesQuery, Result<PageResponse<CellValueResponse>>>
{
    public async Task<Result<PageResponse<CellValueResponse>>> Handle(
        GetAllCellValuesQuery query, CancellationToken ct)
    {
        var total = await db.CellValues.CountAsync(ct);
        var entities = await db.CellValues
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Skip(query.Offset)
            .Take(query.Limit)
            .ToListAsync(ct);

        return new PageResponse<CellValueResponse>
        {
            Items = entities.Select(CellValueMappings.ToResponse).ToList(),
            Count = total
        };
    }
}
