namespace SQLModule.Contracts.Training.Student;

/// <summary>Поле сортировки каталога открытого банка.</summary>
public enum BankTaskSort
{
    /// <summary>Постоянный номер в банке.</summary>
    BankNumber,

    /// <summary>Уровень сложности.</summary>
    Difficulty,

    /// <summary>Дата первого открытия в банке.</summary>
    PublishedAt
}
