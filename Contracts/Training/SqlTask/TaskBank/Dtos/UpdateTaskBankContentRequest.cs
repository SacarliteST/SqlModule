namespace SQLModule.Contracts.Training.SqlTask;

/// <summary>Запрос на изменение авторских материалов задания открытого банка.</summary>
/// <param name="BankHint">Авторская Markdown-подсказка.</param>
/// <param name="ShowSolution">Разрешено ли показывать решение после успешной попытки.</param>
/// <param name="SolutionExplanation">Markdown-пояснение к эталонному решению.</param>
public sealed record UpdateTaskBankContentRequest(
    string? BankHint = null,
    bool? ShowSolution = null,
    string? SolutionExplanation = null);
