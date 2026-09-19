using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.SqlTasks;

public sealed class DeleteSqlTaskEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapDelete(ApiRoutes.Training.SqlTasks.ById, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("DeleteSqlTask")
            .WithTags("Training")
            .WithSummary("Удалить SQL-задание")
            .WithDescription(
                "Удаляет задание, которым никто не пользовался: статус Draft или Archived, нет попыток и прохождений " +
                "студентов (включая платформенные). Одной транзакцией удаляются задание, версии оценки, " +
                "конфигурация оценки и эталонный запрос. Те же правила видны в teacher-details " +
                "(canDelete, deleteBlockReasons). Возвращает 204 No Content.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> Handle(Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<DeleteSqlTaskCommand, Result>(
            new DeleteSqlTaskCommand(id), ct);
        return result.ToNoContent();
    }
}
