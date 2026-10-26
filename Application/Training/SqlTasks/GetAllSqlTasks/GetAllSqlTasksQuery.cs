using SQLModule.Application.Cqrs;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.SqlTask;

namespace SQLModule.Application.Training.SqlTasks;

/// <summary>Запрос списка SQL-заданий для кабинета преподавателя.</summary>
public sealed record GetAllSqlTasksQuery(
    int Offset,
    int Limit,
    Guid? TopicId,
    string? Name,
    Guid? TargetDbId,
    short? DifficultyLevel,
    Domain.Training.PublicationStatus? PublicationStatus)
    : IRequest<Result<PageResponse<SqlTaskResponse>>>;
