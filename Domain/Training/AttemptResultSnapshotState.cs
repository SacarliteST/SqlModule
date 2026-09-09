namespace SQLModule.Domain.Training;

/// <summary>Состояние сохранённого безопасного результата попытки.</summary>
public enum AttemptResultSnapshotState
{
    NotStored = 0,
    Available = 1,
    NotProduced = 2,
    Expired = 3
}
