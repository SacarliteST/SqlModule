using SQLModule.Contracts;
using SQLModule.Contracts.Schema.TargetDb;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Schema.TargetDbs;

public sealed class GetAllTargetDbsEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Schema.TargetDbs.Collection, Handle)
            .WithName("GetAllTargetDbs")
            .WithTags("Schema")
            .WithSummary("Список целевых БД с пагинацией")
            .WithDescription(
                "Возвращает 200 OK со страницей целевых БД, отсортированных по имени. " +
                "offset — количество пропускаемых записей (≥ 0, по умолчанию 0). " +
                "limit — размер страницы (1–100, по умолчанию 20). " +
                "400 — невалидные параметры пагинации.")
            .Produces<PageResponse<TargetDbResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<GetAllTargetDbsRequest>>();
    }

    private static async Task<IResult> Handle(
        [AsParameters] GetAllTargetDbsRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<GetAllTargetDbsQuery, Result<PageResponse<TargetDbResponse>>>(
            new GetAllTargetDbsQuery(request.Offset, request.Limit), ct);
        return result.ToOk();
    }
}
