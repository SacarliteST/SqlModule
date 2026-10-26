using SQLModule.Application.Cqrs;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Student;

namespace SQLModule.Application.Training.Student;

/// <summary>Запрос дерева тем открытого банка.</summary>
/// <param name="Offset">Смещение.</param>
/// <param name="Limit">Размер страницы.</param>
/// <param name="UserId">Идентификатор студента.</param>
public sealed record GetStudentTopicsQuery(int Offset, int Limit, Guid UserId)
    : IRequest<Result<PageResponse<StudentTopicResponse>>>;
