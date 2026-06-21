using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.DbmsCatalog.PhysicalType;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.DbmsCatalog.PhysicalTypes;

internal sealed class CreatePhysicalTypeEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.DbmsCatalog.PhysicalTypes.Collection, Handle)
            .WithName("CreatePhysicalType")
            .WithTags("DbmsCatalog")
            .WithSummary("Создать физический тип данных")
            .WithDescription(
                "Создаёт физический тип данных для указанной СУБД. " +
                "Возвращает 201 Created с телом ответа. " +
                "422 — не прошла валидация (пустое имя или DbmsId, TypeName > 100 символов). " +
                "409 — СУБД с указанным DbmsId не найдена.")
            .Produces<PhysicalTypeResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .AddEndpointFilter<ValidationFilter<CreatePhysicalTypeRequest>>();
    }

    private static async Task<IResult> Handle(
        CreatePhysicalTypeRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<CreatePhysicalTypeCommand, Result<PhysicalTypeResponse>>(
            PhysicalTypeMappings.ToCommand(request), ct);
        return result.ToCreated(r => ApiRoutes.DbmsCatalog.PhysicalTypes.ForId(r.Id));
    }
}
