using SQLModule.Contracts;
using SQLModule.Contracts.Schema.MetaTable;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Schema.MetaTables;

public sealed class GetAllMetaTablesEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Schema.MetaTables.Collection, Handle)
            .WithName("GetAllMetaTables")
            .WithTags("Schema")
            .WithSummary("Список мета-таблиц с пагинацией")
            .WithDescription(
                "Возвращает 200 OK со страницей мета-таблиц, отсортированных по названию. " +
                "offset — количество пропускаемых записей (>= 0, по умолчанию 0). " +
                "limit — размер страницы (1–100, по умолчанию 20). " +
                "targetDbId — необязательный фильтр по целевой БД. " +
                "400 — невалидные параметры пагинации.")
            .Produces<PageResponse<MetaTableResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<GetAllMetaTablesRequest>>();
    }

    private static async Task<IResult> Handle(
        [AsParameters] GetAllMetaTablesRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<GetAllMetaTablesQuery, Result<PageResponse<MetaTableResponse>>>(
            new GetAllMetaTablesQuery(request.Offset, request.Limit, request.TargetDbId), ct);
        return result.ToOk();
    }
}
