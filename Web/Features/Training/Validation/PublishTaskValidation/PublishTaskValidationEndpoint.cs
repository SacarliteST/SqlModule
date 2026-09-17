using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.ComponentModel.DataAnnotations;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Validation;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.Validation.PublishTaskValidation;

internal sealed class PublishTaskValidationEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Training.SqlTasks.ValidationPublish, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("PublishTaskValidation")
            .WithTags("TrainingValidation")
            .Produces<TaskValidationConfigurationResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .AddEndpointFilter<ValidationFilter<PublishTaskValidationRequest>>();
    }

    private static async Task<IResult> Handle(
        Guid taskId,
        PublishTaskValidationRequest request,
        [FromHeader(Name = "Idempotency-Key"), Required] string? idempotencyKey,
        ISender sender,
        CancellationToken ct)
    {
        if (!Guid.TryParseExact(idempotencyKey, "D", out var key))
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]> { ["Idempotency-Key"] = ["Укажите UUID в формате D."] },
                statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        var result = await sender.Send<
            PublishTaskValidationCommand,
            Result<TaskValidationConfigurationResponse>>(
            new PublishTaskValidationCommand(taskId, Guid.Parse(request.Version!), key),
            ct);
        return result.ToOk();
    }
}
