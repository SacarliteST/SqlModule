using SQLModule.Application.Cqrs;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.SqlTask;

namespace SQLModule.Application.Training.Bank;

/// <summary>Применяет одиночную bank-операцию к каждому заданию пакета.</summary>
public sealed class UpdateTaskBankBatchHandler
    : IRequestHandler<UpdateTaskBankBatchCommand, Result<TaskBankBatchResponse>>
{
    private readonly ISender sender;

    /// <summary>Создаёт обработчик пакетной операции.</summary>
    public UpdateTaskBankBatchHandler(ISender sender)
    {
        this.sender = sender;
    }

    /// <inheritdoc />
    public async Task<Result<TaskBankBatchResponse>> Handle(
        UpdateTaskBankBatchCommand command,
        CancellationToken ct)
    {
        var items = new List<TaskBankBatchItemResponse>(command.TaskIds.Count);
        foreach (var taskId in command.TaskIds.Distinct())
        {
            var result = command.Open
                ? await sender.Send<OpenTaskBankCommand, Result<TaskBankStateResponse>>(
                    new OpenTaskBankCommand(taskId), ct)
                : await sender.Send<CloseTaskBankCommand, Result<TaskBankStateResponse>>(
                    new CloseTaskBankCommand(taskId), ct);
            items.Add(result.IsSuccess
                ? new TaskBankBatchItemResponse(taskId, true, result.Value, [])
                : new TaskBankBatchItemResponse(taskId, false, null, ToBlockers(result.Error!)));
        }

        return new TaskBankBatchResponse(
            items.Count(item => item.Updated),
            items.Count(item => !item.Updated),
            items);
    }

    private static IReadOnlyList<TaskBankBlockerResponse> ToBlockers(Error error) =>
        error.Errors is null
            ? [new TaskBankBlockerResponse(error.Code, error.Message)]
            : error.Errors.SelectMany(pair => pair.Value.Select(message =>
                new TaskBankBlockerResponse(pair.Key, message))).ToArray();
}
