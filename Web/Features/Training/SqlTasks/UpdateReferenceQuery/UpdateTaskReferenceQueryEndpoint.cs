using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.SqlTask;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.SqlTasks;

public sealed class UpdateTaskReferenceQueryEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiRoutes.Training.SqlTasks.ReferenceQuery, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("UpdateTaskReferenceQuery")
            .WithTags("Training")
            .WithSummary("Обновить эталонное решение задания")
            .WithDescription(
                "Проверяет SQL в выбранной учебной базе и обновляет собственный эталон Draft-задания. " +
                "Эталон, превышающий серверный лимит сравнения, отклоняется с кодом " +
                "ReferenceResultExceedsComparisonLimit. " +
                "Эталон нельзя изменить после появления попыток. Возвращает сохранённое состояние эталона.")
            .Produces<UpdateTaskReferenceQueryResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .AddEndpointFilter<ValidationFilter<UpdateTaskReferenceQueryRequest>>();
    }

    private static async Task<IResult> Handle(
        Guid id,
        UpdateTaskReferenceQueryRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send<UpdateTaskReferenceQueryCommand, Result<UpdateTaskReferenceQueryResponse>>(
            new UpdateTaskReferenceQueryCommand(
                id,
                request.TargetDbId!.Value,
                request.QueryText!,
                request.StrictColumnOrder!.Value,
                request.StrictRowOrder!.Value),
            ct);
        return result.ToOk();
    }
}
