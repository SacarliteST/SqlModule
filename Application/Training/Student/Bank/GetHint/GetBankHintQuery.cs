using SQLModule.Application.Cqrs;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Student;

namespace SQLModule.Application.Training.Student;

/// <summary>Запрос подсказки к заданию открытого банка.</summary>
/// <param name="TaskId">Идентификатор задания.</param>
/// <param name="UserId">Идентификатор студента.</param>
public sealed record GetBankHintQuery(Guid TaskId, Guid UserId)
    : IRequest<Result<BankHintResponse>>;
