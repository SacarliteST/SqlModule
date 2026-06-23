using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.TargetDb;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.Schema.TargetDbs;

public sealed class UpdateTargetDbEndpoint : IDevEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiRoutes.Schema.TargetDbs.ById, Handle)
            .WithName("UpdateTargetDb")
            .WithTags("Schema", "DevTools")
            .WithSummary("Обновить целевую БД")
            .WithDescription(
                "dev-only: правка структуры схемы в обход CreateSchema. " +
                "Обновляет поля существующей БД-песочницы. " +
                "Возвращает 204 No Content. " +
                "400 — не прошла валидация входных данных. " +
                "404 — запись с указанным id не найдена.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddEndpointFilter<ValidationFilter<UpdateTargetDbRequest>>();
    }

    private static async Task<IResult> Handle(
        Guid id, UpdateTargetDbRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<UpdateTargetDbCommand, Result>(
            TargetDbMappings.ToCommand(id, request), ct);
        return result.ToNoContent();
    }
}
