using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.DataRecord;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.DataRecords;

internal sealed class GetDataRecordByIdEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Schema.DataRecords.ById, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("GetDataRecordById")
            .WithTags("Schema")
            .WithSummary("Получить строку данных по Id")
            .WithDescription(
                "Возвращает строку данных (EAV-якорь) по идентификатору. " +
                "404 — строка с указанным Id не найдена.")
            .Produces<DataRecordResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<GetDataRecordByIdQuery, Result<DataRecordResponse>>(
            new GetDataRecordByIdQuery(id), ct);
        return result.ToOk();
    }
}
