using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Validation;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.Validation.GetTaskValidation;

internal sealed class GetTaskValidationEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Training.SqlTasks.Validation, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("GetTaskValidation")
            .WithTags("TrainingValidation")
            .Produces<TaskValidationConfigurationResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> Handle(Guid taskId, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<
            GetTaskValidationQuery,
            Result<TaskValidationConfigurationResponse>>(
            new GetTaskValidationQuery(taskId),
            ct);
        return result.ToOk();
    }
}
