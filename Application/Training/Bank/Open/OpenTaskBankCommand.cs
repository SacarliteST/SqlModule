using SQLModule.Application.Cqrs;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.SqlTask;

namespace SQLModule.Application.Training.Bank;

/// <summary>Команда открытия задания в банке.</summary>
/// <param name="TaskId">Идентификатор задания.</param>
public sealed record OpenTaskBankCommand(Guid TaskId)
    : IRequest<Result<TaskBankStateResponse>>;
