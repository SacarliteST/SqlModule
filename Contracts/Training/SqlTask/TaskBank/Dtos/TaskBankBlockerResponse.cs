namespace SQLModule.Contracts.Training.SqlTask;

/// <summary>Причина, мешающая открыть задание в банке.</summary>
/// <param name="Code">Стабильный машинный код причины.</param>
/// <param name="Message">Понятное пользователю описание причины.</param>
public sealed record TaskBankBlockerResponse(string Code, string Message);
