namespace SQLModule.Contracts.Training.Student;

/// <summary>Подсказка к заданию открытого банка.</summary>
/// <param name="AttemptNumber">Номер неуспешной попытки, по которой построена подсказка.</param>
/// <param name="AutoHints">Автоматические подсказки по непройденным критериям.</param>
/// <param name="AuthorHint">Авторская Markdown-подсказка.</param>
public sealed record BankHintResponse(
    int AttemptNumber,
    IReadOnlyList<BankAutoHintResponse> AutoHints,
    string? AuthorHint);
