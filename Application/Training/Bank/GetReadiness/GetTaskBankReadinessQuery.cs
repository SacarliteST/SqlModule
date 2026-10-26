using SQLModule.Application.Cqrs;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.SqlTask;

namespace SQLModule.Application.Training.Bank;

/// <summary>Запрос готовности задания к открытию в банке.</summary>
/// <param name="TaskId">Идентификатор задания.</param>
public sealed record GetTaskBankReadinessQuery(Guid TaskId)
    : IRequest<Result<TaskBankReadinessResponse>>;
