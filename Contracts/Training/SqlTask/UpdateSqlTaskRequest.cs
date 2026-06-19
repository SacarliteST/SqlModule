namespace SQLModule.Contracts.Training.SqlTask;

/// <summary>Запрос на обновление SQL-задания (FK не меняются).</summary>
/// <param name="TaskName">Новое название задания (не пустое, не более 300 символов).</param>
/// <param name="TaskText">Новый текст условия задания (не пустое).</param>
/// <param name="DifficultyLevel">Новый уровень сложности (1–5).</param>
public record UpdateSqlTaskRequest(string TaskName, string TaskText, short DifficultyLevel);
