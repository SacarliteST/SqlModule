using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Persistence;
using SQLModule.Application.Training.SqlTasks;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.SqlTask;

namespace SQLModule.Application.Training.Bank;

/// <summary>Закрывает задание в банке, сохраняя назначенные номер и дату.</summary>
public sealed class CloseTaskBankHandler
    : IRequestHandler<CloseTaskBankCommand, Result<TaskBankStateResponse>>
{
    private readonly IApplicationDbContext db;

    /// <summary>Создаёт обработчик закрытия задания в банке.</summary>
    public CloseTaskBankHandler(IApplicationDbContext db)
    {
        this.db = db;
    }

    /// <inheritdoc />
    public async Task<Result<TaskBankStateResponse>> Handle(CloseTaskBankCommand command, CancellationToken ct)
    {
        var task = await db.SqlTasks.SingleOrDefaultAsync(value => value.Id == command.TaskId, ct);
        if (task is null)
        {
            return Result<TaskBankStateResponse>.Fail(SqlTaskErrors.NotFound(command.TaskId));
        }

        task.CloseBank();
        await db.SaveChangesAsync(ct);
        var usedInControl = await db.ModuleSessions.AsNoTracking()
            .AnyAsync(value => value.TaskRef == command.TaskId.ToString(), ct) ||
            await db.Attempts.AsNoTracking()
                .AnyAsync(value => value.TaskId == command.TaskId && value.ModuleSessionId != null, ct);
        return new TaskBankStateResponse(
            task.Visibility.ToContract(),
            task.BankNumber,
            task.BankPublishedAt,
            usedInControl);
    }
}
