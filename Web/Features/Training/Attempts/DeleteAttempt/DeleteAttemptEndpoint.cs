using SQLModule.Application.Cqrs;
using SQLModule.Application.Training.Attempts;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Web.Common;

namespace SQLModule.Web.Features.Training.Attempts;

public sealed class DeleteAttemptEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapDelete(ApiRoutes.Training.Attempts.ById, Handle)
            .RequireAuthorization(Policies.Admin)
            .WithName("DeleteAttempt")
            .WithTags("Training")
            .WithSummary("Удалить попытку")
            .WithDescription(
                "Удаляет запись попытки по Id. " +
                "Тренировочные попытки открытого банка не удаляются. " +
                "Возвращает 204 No Content, 409 для тренировочной попытки, 404 если попытка не найдена.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<DeleteAttemptCommand, Result>(
            new DeleteAttemptCommand(id), ct);
        return result.ToNoContent();
    }
}
