using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Persistence;
using SQLModule.Application.Training.SqlTasks;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.SqlTask;

namespace SQLModule.Application.Training.Bank;

/// <summary>Обрабатывает запрос готовности задания к открытому банку.</summary>
public sealed class GetTaskBankReadinessHandler
    : IRequestHandler<GetTaskBankReadinessQuery, Result<TaskBankReadinessResponse>>
{
    private readonly IApplicationDbContext db;
    private readonly ITaskBankReadiness readiness;

    /// <summary>Создаёт обработчик запроса готовности.</summary>
    public GetTaskBankReadinessHandler(IApplicationDbContext db, ITaskBankReadiness readiness)
    {
        this.db = db;
        this.readiness = readiness;
    }

    /// <inheritdoc />
    public async Task<Result<TaskBankReadinessResponse>> Handle(
        GetTaskBankReadinessQuery query,
        CancellationToken ct)
    {
        if (!await db.SqlTasks.AsNoTracking().AnyAsync(value => value.Id == query.TaskId, ct))
        {
            return Result<TaskBankReadinessResponse>.Fail(SqlTaskErrors.NotFound(query.TaskId));
        }

        return await readiness.EvaluateAsync(query.TaskId, ct);
    }
}
