using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts.Schema.AttributeParameterValue;
using SQLModule.Data.Core;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Schema.AttributeParameterValues;

internal record GetAttributeParameterValueByIdQuery(Guid Id)
    : IRequest<Result<AttributeParameterValueResponse>>;

internal sealed class GetAttributeParameterValueByIdHandler(AppDbContext db)
    : IRequestHandler<GetAttributeParameterValueByIdQuery, Result<AttributeParameterValueResponse>>
{
    public async Task<Result<AttributeParameterValueResponse>> Handle(
        GetAttributeParameterValueByIdQuery query, CancellationToken ct)
    {
        var entity = await db.AttributeParameterValues
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.Id, ct);

        if (entity is null)
        {
            return Result<AttributeParameterValueResponse>.Fail(
                AttributeParameterValueErrors.NotFound(query.Id));
        }

        return AttributeParameterValueMappings.ToResponse(entity);
    }
}
