using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.AttributeParameterValue;
using SQLModule.Application.Persistence;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.Schema.AttributeParameterValues;

public record GetAttributeParameterValueByIdQuery(Guid Id)
    : IRequest<Result<AttributeParameterValueResponse>>;

public sealed class GetAttributeParameterValueByIdHandler(IApplicationDbContext db)
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
