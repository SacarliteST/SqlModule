namespace SQLModule.Contracts.Training.SqlTask;

/// <summary>Параметры запроса списка SQL-заданий с пагинацией.</summary>
/// <param name="Offset">Количество пропускаемых записей. Минимум: 0. По умолчанию: 0.</param>
/// <param name="Limit">Максимальное количество записей на странице. Диапазон: 1–100. По умолчанию: 20.</param>
/// <param name="TopicId">Фильтр по теме.</param>
/// <param name="Name">Поиск по названию.</param>
/// <param name="TargetDbId">Фильтр по учебной базе.</param>
/// <param name="DifficultyLevel">Фильтр по сложности.</param>
/// <param name="PublicationStatus">Фильтр по статусу публикации.</param>
public record GetAllSqlTasksRequest(
    int Offset = 0,
    int Limit = 20,
    Guid? TopicId = null,
    string? Name = null,
    Guid? TargetDbId = null,
    short? DifficultyLevel = null,
    SQLModule.Domain.Training.PublicationStatus? PublicationStatus = null);
