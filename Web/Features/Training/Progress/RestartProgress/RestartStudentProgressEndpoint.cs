using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Validation;
using SQLModule.Domain.Common;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;
using SQLModule.Web.Features.ModuleIntegration;

namespace SQLModule.Web.Features.Training.Progress.RestartProgress;

internal sealed class RestartStudentProgressEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app) =>
        app.MapPost(ApiRoutes.Training.Student.TaskProgressRestart, Handle)
            .RequireAuthorization(Policies.Student)
            .AddEndpointFilter<StandaloneOnlyMutationFilter>()
            .WithName("RestartStudentTaskProgress")
            .WithTags("Student")
            .Produces<StudentTaskProgressResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);

    private static Task<IResult> Handle(
        Guid taskId,
        [FromHeader(Name = "Idempotency-Key"), Required] string? idempotencyKey,
        ICurrentUser currentUser,
        ISender sender,
        CancellationToken ct)
    {
        if (!Guid.TryParseExact(idempotencyKey, "D", out var key))
        {
            return Task.FromResult<IResult>(Results.ValidationProblem(
                new Dictionary<string, string[]> { ["Idempotency-Key"] = ["Укажите UUID в формате D."] },
                statusCode: StatusCodes.Status422UnprocessableEntity));
        }

        return SendAsync(sender, currentUser.UserId!.Value, taskId, key, ct);
    }

    private static async Task<IResult> SendAsync(
        ISender sender, Guid userId, Guid taskId, Guid key, CancellationToken ct) =>
        (await sender.Send<RestartStudentProgressCommand, Result<StudentTaskProgressResponse>>(
            new RestartStudentProgressCommand(userId, taskId, key), ct)).ToOk();
}
