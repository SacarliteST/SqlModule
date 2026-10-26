using SQLModule.Application.Cqrs;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Student;

namespace SQLModule.Application.Training.Student;

/// <summary>Запрос безопасной схемы базы задания.</summary>
public sealed record GetStudentTaskSchemaQuery(Guid TaskId, Guid UserId, Guid? ModuleSessionId)
    : IRequest<Result<StudentTaskSchemaResponse>>;
