using SQLModule.Application.Cqrs;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.SqlTask;

namespace SQLModule.Application.Training.Bank;

/// <summary>Команда изменения авторских материалов задания банка.</summary>
/// <param name="TaskId">Идентификатор задания.</param>
/// <param name="BankHint">Авторская Markdown-подсказка.</param>
/// <param name="ShowSolution">Разрешено ли показывать решение.</param>
/// <param name="SolutionExplanation">Markdown-пояснение к решению.</param>
public sealed record UpdateTaskBankContentCommand(
    Guid TaskId,
    string? BankHint,
    bool ShowSolution,
    string? SolutionExplanation)
    : IRequest<Result<TaskBankContentResponse>>;
