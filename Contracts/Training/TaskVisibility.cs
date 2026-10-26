namespace SQLModule.Contracts.Training;

/// <summary>Определяет назначение опубликованного задания.</summary>
public enum TaskVisibility
{
    /// <summary>Задание доступно только в назначенном платформой контрольном сценарии.</summary>
    Control,

    /// <summary>Задание открыто для самостоятельного решения в банке.</summary>
    Open
}
