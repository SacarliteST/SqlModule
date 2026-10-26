using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Persistence;
using SQLModule.Application.Training.SqlTasks;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.SqlTask;
using SQLModule.Domain.Training;

namespace SQLModule.Application.Training.Bank;

/// <summary>Изменяет авторские материалы задания открытого банка.</summary>
public sealed class UpdateTaskBankContentHandler
    : IRequestHandler<UpdateTaskBankContentCommand, Result<TaskBankContentResponse>>
{
    private readonly IApplicationDbContext db;
    private readonly ITaskBankReadiness readiness;
    private readonly TaskBankOptions options;

    /// <summary>Создаёт обработчик изменения материалов задания.</summary>
    public UpdateTaskBankContentHandler(
        IApplicationDbContext db,
        ITaskBankReadiness readiness,
        IOptions<TaskBankOptions> options)
    {
        this.db = db;
        this.readiness = readiness;
        this.options = options.Value;
    }

    /// <inheritdoc />
    public async Task<Result<TaskBankContentResponse>> Handle(
        UpdateTaskBankContentCommand command,
        CancellationToken ct)
    {
        var task = await db.SqlTasks.SingleOrDefaultAsync(value => value.Id == command.TaskId, ct);
        if (task is null)
        {
            return Result<TaskBankContentResponse>.Fail(SqlTaskErrors.NotFound(command.TaskId));
        }

        if (command.BankHint?.Length > options.HintMaxLength)
        {
            return Result<TaskBankContentResponse>.Fail(Error.Validation(
                "TaskBank.HintTooLong",
                $"Авторская подсказка не должна превышать {options.HintMaxLength} символов.",
                "BankHint",
                options.HintMaxLength));
        }

        if (command.SolutionExplanation?.Length > options.SolutionExplanationMaxLength)
        {
            return Result<TaskBankContentResponse>.Fail(Error.Validation(
                "TaskBank.SolutionExplanationTooLong",
                $"Пояснение не должно превышать {options.SolutionExplanationMaxLength} символов.",
                "SolutionExplanation",
                options.SolutionExplanationMaxLength));
        }

        if (task.Visibility == TaskVisibility.Open &&
            String.IsNullOrWhiteSpace(command.BankHint) &&
            !await readiness.HasAutomaticHintAsync(task.Id, ct))
        {
            return Result<TaskBankContentResponse>.Fail(TaskBankErrors.HintMissing);
        }

        task.UpdateBankContent(command.BankHint, command.ShowSolution, command.SolutionExplanation);
        await db.SaveChangesAsync(ct);
        return new TaskBankContentResponse(task.BankHint, task.ShowSolution, task.SolutionExplanation);
    }
}
