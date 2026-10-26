using SQLModule.Application.Cqrs;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Student;

namespace SQLModule.Application.Training.Student;

/// <summary>Запрос безопасных деталей задания по идентификатору.</summary>
public sealed record GetStudentTaskByIdQuery(Guid TaskId, Guid UserId, Guid? ModuleSessionId)
    : IRequest<Result<StudentTaskDetailsResponse>>;
