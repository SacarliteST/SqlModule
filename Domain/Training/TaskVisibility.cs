namespace SQLModule.Domain.Training;

/// <summary>Назначение опубликованного задания.</summary>
public enum TaskVisibility
{
    /// <summary>Задание доступно только в рамках платформенной сессии.</summary>
    Control,

    /// <summary>Задание опубликовано в открытом банке.</summary>
    Open
}
