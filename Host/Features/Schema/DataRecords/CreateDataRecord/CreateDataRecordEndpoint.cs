using SQLModule.Contracts;
using SQLModule.Contracts.Schema.DataRecord;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Schema.DataRecords;

internal sealed class CreateDataRecordEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Schema.DataRecords.Collection, Handle)
            .WithName("CreateDataRecord")
            .WithTags("Schema")
            .WithSummary("Создать строку данных")
            .WithDescription(
                "Создаёт EAV-якорь строки в указанной мета-таблице. " +
                "Возвращает 201 Created с телом ответа. " +
                "422 — не прошла валидация (пустой MetaTableId, отрицательный SortOrder). " +
                "409 — MetaTable с указанным Id не найдена.")
            .Produces<DataRecordResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .AddEndpointFilter<ValidationFilter<CreateDataRecordRequest>>();
    }

    private static async Task<IResult> Handle(
        CreateDataRecordRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<CreateDataRecordCommand, Result<DataRecordResponse>>(
            DataRecordMappings.ToCommand(request), ct);
        return result.ToCreated(r => ApiRoutes.Schema.DataRecords.ForId(r.Id));
    }
}
