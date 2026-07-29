using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.MetaAttribute;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.MetaAttributes;

internal sealed class GetAllMetaAttributesEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Schema.MetaAttributes.Collection, Handle)
            .WithName("GetAllMetaAttributes")
            .WithTags("Schema")
            .WithSummary("Получить список мета-атрибутов")
            .WithDescription(
                "Возвращает постраничный список мета-атрибутов (колонок). " +
                "Параметры: offset (≥ 0), limit (1–100), metaTableId (опц. — вернуть только " +
                "колонки указанной таблицы). Результат упорядочен по SortOrder, затем по AttributeName. " +
                "422 — не прошла валидация пагинации.")
            .Produces<PageResponse<MetaAttributeResponse>>()
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .AddEndpointFilter<ValidationFilter<GetAllMetaAttributesRequest>>();
    }

    private static async Task<IResult> Handle(
        [AsParameters] GetAllMetaAttributesRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<GetAllMetaAttributesQuery, Result<PageResponse<MetaAttributeResponse>>>(
            new GetAllMetaAttributesQuery(request.Offset, request.Limit, request.MetaTableId), ct);
        return result.ToOk();
    }
}
