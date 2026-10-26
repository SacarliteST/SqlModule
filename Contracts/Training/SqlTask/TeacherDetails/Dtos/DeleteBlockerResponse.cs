namespace SQLModule.Contracts.Training.SqlTask;

/// <summary>Причина, по которой задание нельзя удалить.</summary>
/// <param name="Code">Стабильный machine-код.</param>
/// <param name="Message">Понятное преподавателю описание.</param>
public sealed record DeleteBlockerResponse(string Code, string Message);
