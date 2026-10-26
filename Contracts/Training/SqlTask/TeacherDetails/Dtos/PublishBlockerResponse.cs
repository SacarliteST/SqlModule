namespace SQLModule.Contracts.Training.SqlTask;

/// <summary>Причина, по которой задание нельзя опубликовать или запустить.</summary>
/// <param name="Code">Стабильный machine-код.</param>
/// <param name="Message">Понятное преподавателю описание.</param>
public sealed record PublishBlockerResponse(string Code, string Message);
