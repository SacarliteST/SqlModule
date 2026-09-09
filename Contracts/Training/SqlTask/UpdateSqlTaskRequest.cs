namespace SQLModule.Contracts.Training.SqlTask;

/// <summary>Запрос на обновление SQL-задания.</summary>
/// <param name="TaskName">Новое название задания (не пустое, не более 300 символов).</param>
/// <param name="TaskText">Новый текст условия задания (не пустое).</param>
/// <param name="DifficultyLevel">Новый уровень сложности (1–5).</param>
/// <param name="PublicationStatus">Новый статус публикации; null сохраняет текущий статус.</param>
/// <param name="TopicId">Новая тема; null сохраняет текущую. Смена доступна только для Draft без попыток.</param>
public record UpdateSqlTaskRequest(
    string? TaskName = null,
    string? TaskText = null,
    short? DifficultyLevel = null,
    SQLModule.Domain.Training.PublicationStatus? PublicationStatus = null,
    Guid? TopicId = null);
