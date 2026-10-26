using SQLModule.Application.Cqrs;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.SqlTask;

namespace SQLModule.Application.Training.Bank;

/// <summary>Команда закрытия задания в открытом банке.</summary>
/// <param name="TaskId">Идентификатор задания.</param>
public sealed record CloseTaskBankCommand(Guid TaskId)
    : IRequest<Result<TaskBankStateResponse>>;
