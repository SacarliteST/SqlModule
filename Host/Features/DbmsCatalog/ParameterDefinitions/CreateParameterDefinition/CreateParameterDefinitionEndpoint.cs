using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.DbmsCatalog.ParameterDefinition;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.DbmsCatalog.ParameterDefinitions;

internal sealed class CreateParameterDefinitionEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.DbmsCatalog.ParameterDefinitions.Collection, Handle)
            .WithName("CreateParameterDefinition")
            .WithTags("DbmsCatalog")
            .WithSummary("Создать определение параметра")
            .WithDescription(
                "Создаёт определение параметра для физического типа данных. " +
                "Возвращает 201 Created с телом ответа. " +
                "422 — не прошла валидация (пустые обязательные поля, превышена длина, SortOrder < 0). " +
                "409 — PhysicalType с указанным Id не найден или параметр с таким ParameterKey " +
                "уже существует для этого физического типа.")
            .Produces<ParameterDefinitionResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .AddEndpointFilter<ValidationFilter<CreateParameterDefinitionRequest>>();
    }

    private static async Task<IResult> Handle(
        CreateParameterDefinitionRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<CreateParameterDefinitionCommand, Result<ParameterDefinitionResponse>>(
            ParameterDefinitionMappings.ToCommand(request), ct);
        return result.ToCreated(r => ApiRoutes.DbmsCatalog.ParameterDefinitions.ForId(r.Id));
    }
}
