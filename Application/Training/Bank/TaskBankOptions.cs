namespace SQLModule.Application.Training.Bank;

/// <summary>Ограничения открытого банка заданий.</summary>
public sealed class TaskBankOptions
{
    /// <summary>Имя секции конфигурации.</summary>
    public const string SectionKey = "TaskBank";

    /// <summary>Максимальная длина авторской подсказки.</summary>
    public int HintMaxLength { get; init; } = 4000;

    /// <summary>Максимальная длина пояснения к решению.</summary>
    public int SolutionExplanationMaxLength { get; init; } = 8000;

    /// <summary>Минимальный интервал между отправками решения.</summary>
    public int SubmissionIntervalSeconds { get; init; } = 3;

    /// <summary>Значение Retry-After при слишком частой отправке.</summary>
    public int RetryAfterSeconds { get; init; } = 3;
}
