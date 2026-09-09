using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Attempt;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.Attempts;

public sealed class GetAllAttemptsEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Training.Attempts.Collection, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("GetAllAttempts")
            .WithTags("Training")
            .WithSummary("Список попыток с пагинацией")
            .WithDescription(
                "Возвращает 200 OK со страницей попыток, отсортированных по убыванию даты начала. " +
                "offset — количество пропускаемых записей (>= 0, по умолчанию 0). " +
                "limit — размер страницы (1–100, по умолчанию 20). " +
                "taskId — необязательный фильтр по заданию. " +
                "userId — необязательный фильтр по студенту. " +
                "400 — невалидные параметры пагинации.")
            .Produces<PageResponse<AttemptListItemResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .AddEndpointFilter<ValidationFilter<GetAllAttemptsRequest>>();
    }

    private static async Task<IResult> Handle(
        [AsParameters] GetAllAttemptsRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<GetAllAttemptsQuery, Result<PageResponse<AttemptListItemResponse>>>(
            new GetAllAttemptsQuery(
                request.Offset, request.Limit, request.TaskId, request.UserId,
                request.TopicId, request.Status, request.IsCorrect,
                request.DateFrom, request.DateTo), ct);
        return result.ToOk();
    }
}
