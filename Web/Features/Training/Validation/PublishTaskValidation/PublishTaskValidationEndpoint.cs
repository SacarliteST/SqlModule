using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
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
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send<
            PublishTaskValidationCommand,
            Result<TaskValidationConfigurationResponse>>(
            new PublishTaskValidationCommand(taskId, Guid.Parse(request.Version!)),
            ct);
        return result.ToOk();
    }
}
