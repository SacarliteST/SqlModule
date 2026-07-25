using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.SqlTask;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Auth;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.SqlTasks.GetTeacherTaskDetails;

public sealed class GetTeacherTaskDetailsEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Training.TeacherTasks.Details, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("GetTeacherTaskDetails")
            .WithTags("Training")
            .WithSummary("Получить агрегированные детали задания для преподавателя")
            .WithDescription(
                "Возвращает задание, тему, эталонный запрос, учебную базу, состав таблиц, " +
                "общее число попыток и пять последних попыток.")
            .Produces<TeacherTaskDetailsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(Guid taskId, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<GetTeacherTaskDetailsQuery, Result<TeacherTaskDetailsResponse>>(
            new GetTeacherTaskDetailsQuery(taskId), ct);
        return result.ToOk();
    }
}
