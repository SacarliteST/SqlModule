namespace SQLModule.Contracts.Training.Student;

/// <summary>Разбор решения задания открытого банка.</summary>
/// <param name="SqlText">Эталонный SQL-запрос.</param>
/// <param name="Explanation">Markdown-пояснение преподавателя.</param>
public sealed record BankSolutionResponse(string SqlText, string? Explanation);
