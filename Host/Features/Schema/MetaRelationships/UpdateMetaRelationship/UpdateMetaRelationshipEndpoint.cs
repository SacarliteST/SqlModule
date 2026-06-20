using SQLModule.Contracts;
using SQLModule.Contracts.Schema.MetaRelationship;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Schema.MetaRelationships;

internal sealed class UpdateMetaRelationshipEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiRoutes.Schema.MetaRelationships.ById, Handle)
            .WithName("UpdateMetaRelationship")
            .WithTags("Schema")
            .WithSummary("Обновить связь")
            .WithDescription(
                "Обновляет название и правила поведения FK-связи. FK-колонки (SourceAttributeId, " +
                "TargetAttributeId) не изменяются. " +
                "Возвращает 204 No Content. " +
                "400 — не прошла валидация. " +
                "404 — связь с указанным Id не найдена.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddEndpointFilter<ValidationFilter<UpdateMetaRelationshipRequest>>();
    }

    private static async Task<IResult> Handle(
        Guid id, UpdateMetaRelationshipRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<UpdateMetaRelationshipCommand, Result>(
            MetaRelationshipMappings.ToCommand(id, request), ct);
        return result.ToNoContent();
    }
}
