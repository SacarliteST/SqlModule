using SQLModule.Application.Cqrs;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Student;

namespace SQLModule.Application.Training.Student;

/// <summary>Запрос каталога заданий открытого банка.</summary>
/// <param name="Request">Фильтры каталога.</param>
/// <param name="UserId">Идентификатор студента.</param>
public sealed record GetStudentTasksQuery(StudentTasksRequest Request, Guid UserId)
    : IRequest<Result<PageResponse<StudentTaskResponse>>>;
