using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Training.Bank;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.SqlTask;
using SQLModule.Web.Common;

namespace SQLModule.Web.Features.Training.SqlTasks.TaskBank;

/// <summary>Регистрирует teacher-операции управления открытым банком.</summary>
internal sealed class TaskBankEndpoints : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Training.SqlTasks.BankReadiness, GetReadiness)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("GetTaskBankReadiness")
            .WithTags("Training")
            .WithSummary("Проверить готовность задания к открытому банку")
            .Produces<TaskBankReadinessResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        app.MapPost(ApiRoutes.Training.SqlTasks.BankOpen, Open)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("OpenTaskInBank")
            .WithTags("Training")
            .WithSummary("Открыть задание в банке")
            .WithDescription(
                "Атомарно повторяет проверку готовности, при первом открытии назначает постоянный номер и дату. " +
                "409 содержит все причины отказа в errors. UsedInControl сообщает об известных контрольных запусках.")
            .Produces<TaskBankStateResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        app.MapPost(ApiRoutes.Training.SqlTasks.BankClose, Close)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("CloseTaskInBank")
            .WithTags("Training")
            .WithSummary("Закрыть задание в банке")
            .Produces<TaskBankStateResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        app.MapPut(ApiRoutes.Training.SqlTasks.BankContent, UpdateContent)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("UpdateTaskBankContent")
            .WithTags("Training")
            .WithSummary("Обновить подсказку и пояснение задания банка")
            .Produces<TaskBankContentResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .AddEndpointFilter<ValidationFilter<UpdateTaskBankContentRequest>>();
    }

    private static async Task<IResult> GetReadiness(Guid id, ISender sender, CancellationToken ct) =>
        (await sender.Send<GetTaskBankReadinessQuery, Result<TaskBankReadinessResponse>>(
            new GetTaskBankReadinessQuery(id), ct)).ToOk();

    private static async Task<IResult> Open(Guid id, ISender sender, CancellationToken ct) =>
        (await sender.Send<OpenTaskBankCommand, Result<TaskBankStateResponse>>(
            new OpenTaskBankCommand(id), ct)).ToOk();

    private static async Task<IResult> Close(Guid id, ISender sender, CancellationToken ct) =>
        (await sender.Send<CloseTaskBankCommand, Result<TaskBankStateResponse>>(
            new CloseTaskBankCommand(id), ct)).ToOk();

    private static async Task<IResult> UpdateContent(
        Guid id,
        UpdateTaskBankContentRequest request,
        ISender sender,
        CancellationToken ct) =>
        (await sender.Send<UpdateTaskBankContentCommand, Result<TaskBankContentResponse>>(
            new UpdateTaskBankContentCommand(
                id,
                request.BankHint,
                request.ShowSolution!.Value,
                request.SolutionExplanation),
            ct)).ToOk();
}
