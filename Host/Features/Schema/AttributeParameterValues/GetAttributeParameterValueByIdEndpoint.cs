using SQLModule.Domain.Schema;
using SQLModule.Domain.Exceptions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Domain;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.Schema.AttributeParameterValues;

public sealed class GetAttributeParameterValueByIdEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Schema.AttributeParameterValues.ById, Handle)
            .WithName("GetAttributeParameterValueById")
            .WithTags("Schema")
            .Produces<CreateAttributeParameterValueEndpoint.Response>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<Ok<CreateAttributeParameterValueEndpoint.Response>> Handle(
        Guid id, TemplateDbContext db, CancellationToken ct)
    {
        var entity = await db.AttributeParameterValues
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException(nameof(AttributeParameterValue), id);

        return TypedResults.Ok(CreateAttributeParameterValueEndpoint.ToResponse(entity));
    }
}
