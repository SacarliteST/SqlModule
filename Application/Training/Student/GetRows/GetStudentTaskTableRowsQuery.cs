using SQLModule.Application.Cqrs;
using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.SchemaBuilder;

namespace SQLModule.Application.Training.Student;

/// <summary>Запрос страницы учебных строк таблицы задания.</summary>
public sealed record GetStudentTaskTableRowsQuery(
    Guid TaskId,
    Guid TableId,
    int Offset,
    int Limit,
    Guid UserId,
    Guid? ModuleSessionId)
    : IRequest<Result<TableRowsResponse>>;
