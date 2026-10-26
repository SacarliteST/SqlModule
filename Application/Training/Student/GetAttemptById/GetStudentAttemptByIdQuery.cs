using SQLModule.Application.Cqrs;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Student;

namespace SQLModule.Application.Training.Student;

/// <summary>Запрос безопасных подробностей личной попытки.</summary>
public sealed record GetStudentAttemptByIdQuery(Guid AttemptId, Guid UserId)
    : IRequest<Result<StudentAttemptResponse>>;
