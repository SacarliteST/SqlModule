namespace SQLModule.Contracts.Training.SqlTask;

/// <summary>Сохранённые авторские материалы задания открытого банка.</summary>
/// <param name="BankHint">Авторская Markdown-подсказка.</param>
/// <param name="ShowSolution">Разрешено ли показывать решение после успешной попытки.</param>
/// <param name="SolutionExplanation">Markdown-пояснение к эталонному решению.</param>
public sealed record TaskBankContentResponse(
    string? BankHint,
    bool ShowSolution,
    string? SolutionExplanation);
