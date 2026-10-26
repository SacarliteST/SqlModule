namespace SQLModule.Contracts.Training.Attempt;

/// <summary>Источник попытки в журнале преподавателя.</summary>
public enum AttemptOrigin
{
    /// <summary>Тренировочная попытка открытого банка без платформенной сессии.</summary>
    Training,

    /// <summary>Контрольная попытка в платформенной сессии.</summary>
    Platform
}
