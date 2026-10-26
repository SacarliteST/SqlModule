using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Persistence;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Student;
using SQLModule.Domain.Training;

namespace SQLModule.Application.Training.Student;

/// <summary>Находит открытое задание по номеру и возвращает безопасные детали.</summary>
public sealed class GetStudentTaskByBankNumberHandler
    : IRequestHandler<GetStudentTaskByBankNumberQuery, Result<StudentTaskDetailsResponse>>
{
    private readonly IApplicationDbContext db;
    private readonly ISender sender;

    /// <summary>Создаёт обработчик поиска задания по номеру.</summary>
    public GetStudentTaskByBankNumberHandler(IApplicationDbContext db, ISender sender)
    {
        this.db = db;
        this.sender = sender;
    }

    /// <inheritdoc />
    public async Task<Result<StudentTaskDetailsResponse>> Handle(
        GetStudentTaskByBankNumberQuery query,
        CancellationToken ct)
    {
        var taskId = await db.SqlTasks.AsNoTracking()
            .Where(task => task.BankNumber == query.BankNumber &&
                           task.PublicationStatus == PublicationStatus.Published &&
                           task.Visibility == TaskVisibility.Open)
            .Select(task => (Guid?)task.Id)
            .SingleOrDefaultAsync(ct);
        if (!taskId.HasValue)
        {
            return Result<StudentTaskDetailsResponse>.Fail(StudentErrors.NotFound);
        }

        return await sender.Send<GetStudentTaskByIdQuery, Result<StudentTaskDetailsResponse>>(
            new GetStudentTaskByIdQuery(taskId.Value, query.UserId, null),
            ct);
    }
}
