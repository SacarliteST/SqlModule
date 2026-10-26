using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Cqrs;
using SQLModule.Application.ModuleIntegration;
using SQLModule.Application.Persistence;
using SQLModule.Application.Schema.SchemaBuilder.GetTableRows;
using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Domain.Training;

namespace SQLModule.Application.Training.Student;

/// <summary>Возвращает страницу учебных данных только из базы доступного задания.</summary>
public sealed class GetStudentTaskTableRowsHandler
    : IRequestHandler<GetStudentTaskTableRowsQuery, Result<TableRowsResponse>>
{
    private readonly IApplicationDbContext db;
    private readonly ISender sender;
    private readonly IStudentTaskAccess taskAccess;

    /// <summary>Создаёт обработчик строк таблицы.</summary>
    public GetStudentTaskTableRowsHandler(
        IApplicationDbContext db,
        ISender sender,
        IStudentTaskAccess taskAccess)
    {
        this.db = db;
        this.sender = sender;
        this.taskAccess = taskAccess;
    }

    /// <inheritdoc />
    public async Task<Result<TableRowsResponse>> Handle(GetStudentTaskTableRowsQuery query, CancellationToken ct)
    {
        var access = await taskAccess.EnsureAsync(
            query.UserId, query.ModuleSessionId, query.TaskId, PlatformScopeMode.Read, ct);
        if (!access.IsSuccess)
        {
            return Result<TableRowsResponse>.Fail(access.Error!);
        }

        var targetDbId = await db.SqlTasks.AsNoTracking()
            .Where(task => task.Id == query.TaskId && task.PublicationStatus == PublicationStatus.Published &&
                           (query.ModuleSessionId.HasValue || task.Visibility == TaskVisibility.Open))
            .Select(task => (Guid?)task.SqlQuery.TargetDbId)
            .SingleOrDefaultAsync(ct);
        if (!targetDbId.HasValue)
        {
            return Result<TableRowsResponse>.Fail(StudentErrors.NotFound);
        }

        return await sender.Send<GetTableRowsQuery, Result<TableRowsResponse>>(
            new GetTableRowsQuery(targetDbId.Value, query.TableId, query.Offset, query.Limit), ct);
    }
}
