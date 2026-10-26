using SQLModule.Application.Cqrs;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Student;

namespace SQLModule.Application.Training.Student;

/// <summary>Запрос личной истории попыток студента.</summary>
public sealed record GetStudentAttemptsQuery(StudentAttemptsRequest Request, Guid UserId)
    : IRequest<Result<PageResponse<StudentAttemptListItemResponse>>>;
