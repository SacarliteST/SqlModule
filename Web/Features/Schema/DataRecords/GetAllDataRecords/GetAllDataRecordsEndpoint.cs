using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.DataRecord;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.DataRecords;

internal sealed class GetAllDataRecordsEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Schema.DataRecords.Collection, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("GetAllDataRecords")
            .WithTags("Schema")
            .WithSummary("Получить список строк данных")
            .WithDescription(
                "Возвращает постраничный список EAV-строк. " +
                "Параметры: offset (≥ 0), limit (1–100), metaTableId (опц. — вернуть только " +
                "строки указанной таблицы). Результат упорядочен по SortOrder, затем по Id. " +
                "422 — не прошла валидация пагинации.")
            .Produces<PageResponse<DataRecordResponse>>()
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .AddEndpointFilter<ValidationFilter<GetAllDataRecordsRequest>>();
    }

    private static async Task<IResult> Handle(
        [AsParameters] GetAllDataRecordsRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<GetAllDataRecordsQuery, Result<PageResponse<DataRecordResponse>>>(
            new GetAllDataRecordsQuery(request.Offset, request.Limit, request.MetaTableId), ct);
        return result.ToOk();
    }
}
