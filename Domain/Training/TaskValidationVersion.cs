using SQLModule.Domain.Common;

namespace SQLModule.Domain.Training;

/// <summary>Неизменяемая опубликованная версия правил и данных проверки.</summary>
public sealed class TaskValidationVersion : BaseEntity
{
    public Guid TaskId { get; private set; }
    public int VersionNumber { get; private set; }
    public Guid ConfigurationVersion { get; private set; }
    public int PassingScore { get; private set; }
    public int? MaxAttempts { get; private set; }
    public long VisibleHintGroupsMask { get; private set; }
    public string SchemaSnapshotJson { get; private set; }
    public string DatasetSnapshotJson { get; private set; }
    public string ReferenceQuerySnapshotJson { get; private set; }
    public string ExpectedResultSnapshotJson { get; private set; }
    public string ValidationConfigurationSnapshotJson { get; private set; }
    public string AnalyzerVersion { get; private set; }
    public DateTimeOffset PublishedAt { get; private set; }
    public Guid PublishedById { get; private set; }
    public string PublishedByName { get; private set; }

    public SqlTask Task { get; private set; } = null!;

    private TaskValidationVersion(
        Guid id,
        Guid taskId,
        int versionNumber,
        Guid configurationVersion,
        int passingScore,
        int? maxAttempts,
        long visibleHintGroupsMask,
        string schemaSnapshotJson,
        string datasetSnapshotJson,
        string referenceQuerySnapshotJson,
        string expectedResultSnapshotJson,
        string validationConfigurationSnapshotJson,
        string analyzerVersion,
        Guid publishedById,
        string publishedByName,
        DateTimeOffset publishedAt) : base(id)
    {
        TaskId = taskId;
        VersionNumber = versionNumber;
        ConfigurationVersion = configurationVersion;
        PassingScore = passingScore;
        MaxAttempts = maxAttempts;
        VisibleHintGroupsMask = visibleHintGroupsMask;
        SchemaSnapshotJson = schemaSnapshotJson;
        DatasetSnapshotJson = datasetSnapshotJson;
        ReferenceQuerySnapshotJson = referenceQuerySnapshotJson;
        ExpectedResultSnapshotJson = expectedResultSnapshotJson;
        ValidationConfigurationSnapshotJson = validationConfigurationSnapshotJson;
        AnalyzerVersion = analyzerVersion;
        PublishedAt = publishedAt;
        PublishedById = publishedById;
        PublishedByName = publishedByName;
    }

    public static TaskValidationVersion Publish(
        Guid taskId,
        int versionNumber,
        Guid configurationVersion,
        int passingScore,
        int? maxAttempts,
        long visibleHintGroupsMask,
        TaskValidationVersionSnapshot snapshot,
        Guid publishedById,
        string publishedByName,
        DateTimeOffset publishedAt,
        Guid? id = null) =>
        new(
            id ?? Guid.NewGuid(), taskId, versionNumber, configurationVersion,
            passingScore, maxAttempts, visibleHintGroupsMask,
            snapshot.SchemaJson, snapshot.DatasetJson, snapshot.ReferenceQueryJson,
            snapshot.ExpectedResultJson, snapshot.ConfigurationJson, snapshot.AnalyzerVersion,
            publishedById, publishedByName, publishedAt);

    public IReadOnlyList<HintGroup> GetVisibleHintGroups() =>
        Enum.GetValues<HintGroup>()
            .Where(group => (VisibleHintGroupsMask & (1L << (int)group)) != 0)
            .ToArray();
}
