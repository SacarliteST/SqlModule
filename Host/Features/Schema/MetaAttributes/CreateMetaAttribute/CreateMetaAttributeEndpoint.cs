using SQLModule.Contracts;
using SQLModule.Contracts.Schema.MetaAttribute;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Schema.MetaAttributes;

internal sealed class CreateMetaAttributeEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Schema.MetaAttributes.Collection, Handle)
            .WithName("CreateMetaAttribute")
            .WithTags("Schema")
            .WithSummary("Создать мета-атрибут (колонку)")
            .WithDescription(
                "Создаёт мета-атрибут (колонку) в указанной мета-таблице. " +
                "Возвращает 201 Created с телом ответа. " +
                "422 — не прошла валидация (пустое имя, пустые Id, SortOrder < 0). " +
                "409 — MetaTable или PhysicalType с указанным Id не найден.")
            .Produces<MetaAttributeResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .AddEndpointFilter<ValidationFilter<CreateMetaAttributeRequest>>();
    }

    private static async Task<IResult> Handle(
        CreateMetaAttributeRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<CreateMetaAttributeCommand, Result<MetaAttributeResponse>>(
            MetaAttributeMappings.ToCommand(request), ct);
        return result.ToCreated(r => ApiRoutes.Schema.MetaAttributes.ForId(r.Id));
    }
}
