using SQLModule.Application.Cqrs;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.SqlTask;

namespace SQLModule.Application.Training.SqlTasks.GetTeacherTaskDetails;

/// <summary>Запрос агрегированных деталей SQL-задания для преподавателя.</summary>
/// <param name="TaskId">Идентификатор задания.</param>
public sealed record GetTeacherTaskDetailsQuery(Guid TaskId)
    : IRequest<Result<TeacherTaskDetailsResponse>>;
