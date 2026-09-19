using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Validation;
using SQLModule.Domain.Common;
using SQLModule.Web.Common;
using SQLModule.Web.Features.ModuleIntegration;

namespace SQLModule.Web.Features.Training.Progress;

internal sealed class FinalizeStudentProgressEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app) =>
        app.MapPost(ApiRoutes.Training.Student.TaskProgressFinalize, Handle)
            .RequireAuthorization(Policies.Student)
            .AddEndpointFilter<StandaloneOnlyMutationFilter>()
            .WithName("FinalizeStudentTaskProgress")
            .WithTags("Student")
            .WithSummary("Завершить standalone-прохождение")
            .WithDescription("Фиксирует фактический BestScore. Передать итоговый балл с клиента нельзя.")
            .Produces<ProgressFinalizationResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);

    private static async Task<IResult> Handle(
        Guid taskId,
        [FromHeader(Name = "Idempotency-Key"), Required] string? idempotencyKey,
        ICurrentUser currentUser,
        IProgressFinalizationService service,
        CancellationToken ct)
    {
        if (!Guid.TryParseExact(idempotencyKey, "D", out var key))
        {
            return InvalidKey();
        }

        return (await service.FinalizeStandaloneAsync(
            currentUser.UserId!.Value, taskId, key, ct)).ToOk();
    }

    private static IResult InvalidKey() => Results.ValidationProblem(
        new Dictionary<string, string[]> { ["Idempotency-Key"] = ["Укажите UUID в формате D."] },
        statusCode: StatusCodes.Status422UnprocessableEntity);
}
