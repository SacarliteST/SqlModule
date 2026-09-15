using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Validation;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.Validation.PreviewTaskValidation;

internal sealed class PreviewTaskValidationEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Training.SqlTasks.ValidationPreview, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("PreviewTaskValidation")
            .WithTags("TrainingValidation")
            .Produces<TaskValidationPreviewResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .AddEndpointFilter<ValidationFilter<TaskValidationPreviewRequest>>();
    }

    private static async Task<IResult> Handle(
        Guid taskId,
        TaskValidationPreviewRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send<
            PreviewTaskValidationCommand,
            Result<TaskValidationPreviewResponse>>(
            new PreviewTaskValidationCommand(
                taskId,
                TaskValidationMappings.ToDefinition(request),
                TaskValidationMappings.ToIdentities(request)),
            ct);
        return result.ToOk();
    }
}
