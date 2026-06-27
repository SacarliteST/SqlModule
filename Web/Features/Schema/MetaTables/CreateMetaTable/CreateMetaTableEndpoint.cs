using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.MetaTable;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.MetaTables;

public sealed class CreateMetaTableEndpoint : IDevEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Schema.MetaTables.Collection, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("CreateMetaTable")
            .WithTags("Schema", "DevTools")
            .WithSummary("Создать мета-таблицу")
            .WithDescription(
                "dev-only: правка структуры схемы в обход CreateSchema. " +
                "Создаёт описание таблицы в схеме данных указанной целевой БД. " +
                "Возвращает 201 Created с телом ответа. " +
                "400 — не прошла валидация входных данных. " +
                "409 — целевая БД с указанным targetDbId не найдена.")
            .Produces<MetaTableResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .AddEndpointFilter<ValidationFilter<CreateMetaTableRequest>>();
    }

    private static async Task<IResult> Handle(
        CreateMetaTableRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<CreateMetaTableCommand, Result<MetaTableResponse>>(
            MetaTableMappings.ToCommand(request), ct);
        return result.ToCreated(r => ApiRoutes.Schema.MetaTables.ForId(r.Id));
    }
}
