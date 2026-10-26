namespace SQLModule.Contracts.Training.Student;

/// <summary>Параметры чтения дерева тем открытого банка.</summary>
/// <param name="Offset">Смещение.</param>
/// <param name="Limit">Размер страницы.</param>
public sealed record StudentTopicsRequest(int Offset = 0, int Limit = 100);
