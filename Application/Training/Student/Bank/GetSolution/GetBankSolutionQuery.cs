using SQLModule.Application.Cqrs;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Student;

namespace SQLModule.Application.Training.Student;

/// <summary>Запрос разбора решения задания открытого банка.</summary>
/// <param name="TaskId">Идентификатор задания.</param>
/// <param name="UserId">Идентификатор студента.</param>
public sealed record GetBankSolutionQuery(Guid TaskId, Guid UserId)
    : IRequest<Result<BankSolutionResponse>>;
