namespace SQLModule.Domain.Training;

/// <summary>Состояние зарезервированного номера попытки.</summary>
public enum AttemptReservationState
{
    Reserved,
    Completed,
    Released
}
