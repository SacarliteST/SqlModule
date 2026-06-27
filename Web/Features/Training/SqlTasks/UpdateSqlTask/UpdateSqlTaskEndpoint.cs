using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.SqlTask;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.SqlTasks;

public sealed class UpdateSqlTaskEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiRoutes.Training.SqlTasks.ById, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("UpdateSqlTask")
            .WithTags("Training")
            .WithSummary("Обновить SQL-задание")
            .WithDescription(
                "Обновляет название, текст и сложность задания (FK не меняются). " +
                "Возвращает 204 No Content. " +
                "400 — не прошла валидация. " +
                "404 — задание с указанным id не найдено.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddEndpointFilter<ValidationFilter<UpdateSqlTaskRequest>>();
    }

    private static async Task<IResult> Handle(
        Guid id, UpdateSqlTaskRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<UpdateSqlTaskCommand, Result>(
            SqlTaskMappings.ToCommand(id, request), ct);
        return result.ToNoContent();
    }
}
