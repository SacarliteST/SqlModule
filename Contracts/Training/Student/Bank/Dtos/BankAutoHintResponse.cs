namespace SQLModule.Contracts.Training.Student;

/// <summary>Безопасная автоматическая подсказка по группе критериев.</summary>
/// <param name="Group">Группа подсказки.</param>
/// <param name="Message">Текст без баллов и эталонных значений.</param>
public sealed record BankAutoHintResponse(HintGroup Group, string Message);
