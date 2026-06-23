namespace SQLModule.Contracts.Training.SqlTask;

/// <summary>Запрос на создание SQL-задания.</summary>
/// <param name="TopicId">Идентификатор темы, к которой относится задание.</param>
/// <param name="SqlQueryId">Идентификатор эталонного SQL-запроса (определяет целевую БД).</param>
/// <param name="TaskName">Название задания (не пустое, не более 300 символов).</param>
/// <param name="TaskText">Текст условия задания (не пустое).</param>
/// <param name="DifficultyLevel">Уровень сложности задания (1–5).</param>
public record CreateSqlTaskRequest(
    Guid TopicId,
    Guid SqlQueryId,
    string TaskName,
    string TaskText,
    short DifficultyLevel);
