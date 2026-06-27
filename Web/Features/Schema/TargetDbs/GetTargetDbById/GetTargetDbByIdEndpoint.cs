using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.TargetDb;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.TargetDbs;

public sealed class GetTargetDbByIdEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Schema.TargetDbs.ById, Handle)
            .WithName("GetTargetDbById")
            .WithTags("Schema")
            .WithSummary("Получить целевую БД по Id")
            .WithDescription(
                "Возвращает 200 OK с данными БД. " +
                "404 — запись с указанным id не найдена.")
            .Produces<TargetDbResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<GetTargetDbByIdQuery, Result<TargetDbResponse>>(
            new GetTargetDbByIdQuery(id), ct);
        return result.ToOk();
    }
}
