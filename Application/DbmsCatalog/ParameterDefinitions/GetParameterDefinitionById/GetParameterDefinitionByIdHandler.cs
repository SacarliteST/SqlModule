using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.DbmsCatalog.ParameterDefinition;
using SQLModule.Application.Persistence;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.DbmsCatalog.ParameterDefinitions;

public record GetParameterDefinitionByIdQuery(Guid Id) : IRequest<Result<ParameterDefinitionResponse>>;

public sealed class GetParameterDefinitionByIdHandler(IApplicationDbContext db)
    : IRequestHandler<GetParameterDefinitionByIdQuery, Result<ParameterDefinitionResponse>>
{
    public async Task<Result<ParameterDefinitionResponse>> Handle(
        GetParameterDefinitionByIdQuery query, CancellationToken ct)
    {
        var entity = await db.ParameterDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.Id, ct);

        if (entity is null)
        {
            return Result<ParameterDefinitionResponse>.Fail(ParameterDefinitionErrors.NotFound(query.Id));
        }

        return ParameterDefinitionMappings.ToResponse(entity);
    }
}
