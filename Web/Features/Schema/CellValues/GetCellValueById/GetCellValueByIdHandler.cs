using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.CellValue;
using SQLModule.Data.Core;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.CellValues;

internal record GetCellValueByIdQuery(Guid Id) : IRequest<Result<CellValueResponse>>;

internal sealed class GetCellValueByIdHandler(AppDbContext db)
    : IRequestHandler<GetCellValueByIdQuery, Result<CellValueResponse>>
{
    public async Task<Result<CellValueResponse>> Handle(GetCellValueByIdQuery query, CancellationToken ct)
    {
        var entity = await db.CellValues
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.Id, ct);

        if (entity is null)
        {
            return Result<CellValueResponse>.Fail(CellValueErrors.NotFound(query.Id));
        }

        return CellValueMappings.ToResponse(entity);
    }
}
