using SQLModule.Contracts;
using SQLModule.Contracts.Training.Attempt;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Training.Attempts;

public sealed class GetAllAttemptsEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Training.Attempts.Collection, Handle)
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
            .Produces<PageResponse<AttemptResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<GetAllAttemptsRequest>>();
    }

    private static async Task<IResult> Handle(
        [AsParameters] GetAllAttemptsRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<GetAllAttemptsQuery, Result<PageResponse<AttemptResponse>>>(
            new GetAllAttemptsQuery(request.Offset, request.Limit, request.TaskId, request.UserId), ct);
        return result.ToOk();
    }
}
