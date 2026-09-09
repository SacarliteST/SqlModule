namespace SQLModule.Web.Features.Training.Attempts;

/// <summary>Ограничения безопасного snapshot фактического результата попытки.</summary>
public sealed class AttemptResultSnapshotsOptions
{
    public const string SectionKey = "AttemptResultSnapshots";

    public int RetentionDays { get; init; } = 30;
    public int MaxRows { get; init; } = 200;
    public int MaxColumns { get; init; } = 100;
    public int MaxCellLength { get; init; } = 16384;
    public int MaxSerializedBytes { get; init; } = 1048576;
}
