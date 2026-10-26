using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Persistence;
using SQLModule.Application.Training.SqlTasks;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.SqlTask;
using SQLModule.Domain.Training;

namespace SQLModule.Application.Training.Bank;

/// <summary>Атомарно открывает задание в банке и назначает постоянный номер.</summary>
public sealed class OpenTaskBankHandler
    : IRequestHandler<OpenTaskBankCommand, Result<TaskBankStateResponse>>
{
    private readonly IApplicationDbContext db;
    private readonly ITaskBankReadiness readiness;
    private readonly TimeProvider timeProvider;

    /// <summary>Создаёт обработчик открытия задания в банке.</summary>
    public OpenTaskBankHandler(
        IApplicationDbContext db,
        ITaskBankReadiness readiness,
        TimeProvider timeProvider)
    {
        this.db = db;
        this.readiness = readiness;
        this.timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<Result<TaskBankStateResponse>> Handle(OpenTaskBankCommand command, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await LockTaskAsync(db, command.TaskId, ct);
        var task = await db.SqlTasks.SingleOrDefaultAsync(value => value.Id == command.TaskId, ct);
        if (task is null)
        {
            await transaction.RollbackAsync(ct);
            return Result<TaskBankStateResponse>.Fail(SqlTaskErrors.NotFound(command.TaskId));
        }

        var state = await readiness.EvaluateAsync(command.TaskId, ct);
        if (!state.CanOpen)
        {
            await transaction.RollbackAsync(ct);
            return Result<TaskBankStateResponse>.Fail(ToConflict(state.Blockers));
        }

        if (task.Visibility != TaskVisibility.Open)
        {
            var bankNumber = task.BankNumber ?? await NextBankNumberAsync(db, ct);
            task.OpenInBank(bankNumber, timeProvider.GetUtcNow());
            await db.SaveChangesAsync(ct);
            await db.Entry(task).ReloadAsync(ct);
        }

        await transaction.CommitAsync(ct);
        return new TaskBankStateResponse(
            task.Visibility.ToContract(),
            task.BankNumber,
            task.BankPublishedAt,
            state.UsedInControl);
    }

    private static Error ToConflict(IReadOnlyList<TaskBankBlockerResponse> blockers) =>
        Error.Conflict(blockers[0].Code, blockers[0].Message) with
        {
            Errors = blockers.ToDictionary(value => value.Code, value => new[] { value.Message })
        };

    private static async Task<long> NextBankNumberAsync(IApplicationDbContext db, CancellationToken ct) =>
        await db.Database.SqlQueryRaw<long>(
            "SELECT nextval('\"SqlTaskBankNumberSequence\"') AS \"Value\"").SingleAsync(ct);

    private static async Task LockTaskAsync(
        IApplicationDbContext db,
        Guid taskId,
        CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.Transaction = db.Database.CurrentTransaction!.GetDbTransaction();
        command.CommandText = "SELECT \"Id\" FROM \"SqlTasks\" WHERE \"Id\" = @taskId FOR UPDATE";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "taskId";
        parameter.Value = taskId;
        command.Parameters.Add(parameter);
        _ = await command.ExecuteScalarAsync(ct);
    }
}
