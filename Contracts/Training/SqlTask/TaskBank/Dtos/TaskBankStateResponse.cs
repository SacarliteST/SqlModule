namespace SQLModule.Contracts.Training.SqlTask;

/// <summary>Текущее состояние задания в открытом банке.</summary>
/// <param name="Visibility">Назначение задания.</param>
/// <param name="BankNumber">Постоянный номер задания в банке.</param>
/// <param name="BankPublishedAt">Дата первого открытия задания в банке.</param>
/// <param name="UsedInControl">Использовалось ли задание в платформенном контрольном сценарии.</param>
public sealed record TaskBankStateResponse(
    TaskVisibility Visibility,
    long? BankNumber,
    DateTimeOffset? BankPublishedAt,
    bool UsedInControl);
