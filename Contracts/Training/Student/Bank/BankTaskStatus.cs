namespace SQLModule.Contracts.Training.Student;

/// <summary>Личный статус задания в открытом банке.</summary>
public enum BankTaskStatus
{
    /// <summary>Тренировочных попыток ещё нет.</summary>
    NotStarted,

    /// <summary>Есть попытки, но проходной балл ещё не набран.</summary>
    InProgress,

    /// <summary>Проходной балл набран хотя бы один раз.</summary>
    Solved
}
