using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.AttributeParameterValue;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.AttributeParameterValues;

internal sealed class GetAttributeParameterValueByIdEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Schema.AttributeParameterValues.ById, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("GetAttributeParameterValueById")
            .WithTags("Schema")
            .WithSummary("Получить значение параметра атрибута по Id")
            .WithDescription(
                "Возвращает значение параметра физического типа по идентификатору. " +
                "404 — запись с указанным Id не найдена.")
            .Produces<AttributeParameterValueResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<
            GetAttributeParameterValueByIdQuery,
            Result<AttributeParameterValueResponse>>(
            new GetAttributeParameterValueByIdQuery(id), ct);
        return result.ToOk();
    }
}
