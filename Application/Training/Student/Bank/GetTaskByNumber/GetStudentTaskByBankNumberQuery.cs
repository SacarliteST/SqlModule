using SQLModule.Application.Cqrs;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Student;

namespace SQLModule.Application.Training.Student;

/// <summary>Запрос задания открытого банка по постоянному номеру.</summary>
/// <param name="BankNumber">Номер задания в банке.</param>
/// <param name="UserId">Идентификатор студента.</param>
public sealed record GetStudentTaskByBankNumberQuery(long BankNumber, Guid UserId)
    : IRequest<Result<StudentTaskDetailsResponse>>;
