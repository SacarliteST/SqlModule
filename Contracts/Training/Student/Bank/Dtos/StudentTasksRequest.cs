namespace SQLModule.Contracts.Training.Student;

/// <summary>Фильтры каталога заданий открытого банка.</summary>
/// <param name="TopicId">Тема с включением всех дочерних тем.</param>
/// <param name="DifficultyLevel">Уровень сложности.</param>
/// <param name="Solved">Фильтр по личному признаку решения.</param>
/// <param name="BankNumber">Точный номер задания в банке.</param>
/// <param name="Search">Поиск по названию и условию.</param>
/// <param name="SortBy">Поле сортировки.</param>
/// <param name="Descending">Обратный порядок сортировки.</param>
/// <param name="Offset">Смещение.</param>
/// <param name="Limit">Размер страницы, не более 50.</param>
public sealed record StudentTasksRequest(
    Guid? TopicId = null,
    short? DifficultyLevel = null,
    bool? Solved = null,
    long? BankNumber = null,
    string? Search = null,
    BankTaskSort SortBy = BankTaskSort.BankNumber,
    bool Descending = false,
    int Offset = 0,
    int Limit = 20);
