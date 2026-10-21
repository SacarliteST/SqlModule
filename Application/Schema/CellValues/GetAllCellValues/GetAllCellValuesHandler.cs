using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.CellValue;
using SQLModule.Application.Persistence;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.Schema.CellValues;

public record GetAllCellValuesQuery(int Offset, int Limit)
    : IRequest<Result<PageResponse<CellValueResponse>>>;

public sealed class GetAllCellValuesHandler(IApplicationDbContext db)
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
