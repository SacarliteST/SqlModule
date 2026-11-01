using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Persistence;
using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.CellValue;

namespace SQLModule.Application.Schema.CellValues;

public record GetCellValueByIdQuery(Guid Id) : IRequest<Result<CellValueResponse>>;

public sealed class GetCellValueByIdHandler(IApplicationDbContext db)
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
