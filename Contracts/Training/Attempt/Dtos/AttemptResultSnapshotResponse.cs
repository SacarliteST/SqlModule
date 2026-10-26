namespace SQLModule.Contracts.Training.Attempt;

/// <summary>Сохранённый безопасный фактический результат попытки.</summary>
public sealed record AttemptResultSnapshotResponse(
    AttemptResultSnapshotState ResultSnapshotState,
    IReadOnlyList<string>? ActualColumns,
    IReadOnlyList<IReadOnlyList<string?>>? ActualRows,
    int? ReturnedRowCount,
    bool IsResultTruncated,
    int? ResultRowLimit,
    DateTimeOffset? ResultSnapshotCreatedAt,
    DateTimeOffset? ResultSnapshotExpiresAt);
