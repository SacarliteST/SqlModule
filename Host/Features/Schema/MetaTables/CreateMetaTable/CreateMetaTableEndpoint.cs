using SQLModule.Contracts;
using SQLModule.Contracts.Schema.MetaTable;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Schema.MetaTables;

public sealed class CreateMetaTableEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Schema.MetaTables.Collection, Handle)
            .WithName("CreateMetaTable")
            .WithTags("Schema")
            .WithSummary("Создать мета-таблицу")
            .WithDescription(
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
