using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.MetaRelationship;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.MetaRelationships;

internal sealed class CreateMetaRelationshipEndpoint : IDevEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Schema.MetaRelationships.Collection, Handle)
            .WithName("CreateMetaRelationship")
            .WithTags("Schema", "DevTools")
            .WithSummary("Создать связь между мета-атрибутами")
            .WithDescription(
                "dev-only: правка структуры схемы в обход CreateSchema. " +
                "Создаёт Foreign Key-связь между двумя мета-атрибутами. " +
                "Возвращает 201 Created с телом ответа. " +
                "400 — не прошла валидация (пустое имя, пустые Id, Source == Target, правило > 50 симв.). " +
                "409 — SourceAttribute или TargetAttribute с указанным Id не найден.")
            .Produces<MetaRelationshipResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .AddEndpointFilter<ValidationFilter<CreateMetaRelationshipRequest>>();
    }

    private static async Task<IResult> Handle(
        CreateMetaRelationshipRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<CreateMetaRelationshipCommand, Result<MetaRelationshipResponse>>(
            MetaRelationshipMappings.ToCommand(request), ct);
        return result.ToCreated(r => ApiRoutes.Schema.MetaRelationships.ForId(r.Id));
    }
}
