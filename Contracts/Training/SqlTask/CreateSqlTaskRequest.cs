namespace SQLModule.Contracts.Training.SqlTask;

/// <summary>Запрос на создание SQL-задания.</summary>
/// <param name="TopicId">Идентификатор темы, к которой относится задание.</param>
/// <param name="TaskName">Название задания (не пустое, не более 300 символов).</param>
/// <param name="TaskText">Текст условия задания (не пустое).</param>
/// <param name="DifficultyLevel">Уровень сложности задания (1–5).</param>
/// <param name="ReferenceQuery">Новое эталонное решение задания.</param>
public record CreateSqlTaskRequest(
    Guid? TopicId,
    string? TaskName,
    string? TaskText,
    short? DifficultyLevel,
    ReferenceQueryRequest? ReferenceQuery);
