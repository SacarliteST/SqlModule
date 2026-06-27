using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.DbmsCatalog.ParameterDefinition;
using SQLModule.Data.Core;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.DbmsCatalog.ParameterDefinitions;

internal record GetParameterDefinitionByIdQuery(Guid Id) : IRequest<Result<ParameterDefinitionResponse>>;

internal sealed class GetParameterDefinitionByIdHandler(AppDbContext db)
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
