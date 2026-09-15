using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Validation;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.Validation.UpdateTaskValidation;

internal sealed class UpdateTaskValidationEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiRoutes.Training.SqlTasks.Validation, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("UpdateTaskValidation")
            .WithTags("TrainingValidation")
            .Produces<TaskValidationConfigurationResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .AddEndpointFilter<ValidationFilter<TaskValidationConfigurationRequest>>();
    }

    private static async Task<IResult> Handle(
        Guid taskId,
        TaskValidationConfigurationRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send<
            UpdateTaskValidationCommand,
            Result<TaskValidationConfigurationResponse>>(
            new UpdateTaskValidationCommand(
                taskId,
                Guid.Parse(request.Version!),
                TaskValidationMappings.ToDefinition(request)),
            ct);
        return result.ToOk();
    }
}
