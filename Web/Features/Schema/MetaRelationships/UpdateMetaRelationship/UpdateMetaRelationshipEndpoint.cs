using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.MetaRelationship;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.MetaRelationships;

internal sealed class UpdateMetaRelationshipEndpoint : IDevEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiRoutes.Schema.MetaRelationships.ById, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("UpdateMetaRelationship")
            .WithTags("Schema", "DevTools")
            .WithSummary("Обновить связь")
            .WithDescription(
                "dev-only: правка структуры схемы в обход CreateSchema. " +
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
