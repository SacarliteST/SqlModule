using SQLModule.Domain.Common;

namespace SQLModule.Domain.Training;

/// <summary>Задание SQL-тренажёра.</summary>
public sealed class SqlTask : AuditableEntity
{
    public Guid TopicId { get; private set; }
    public Guid SqlQueryId { get; private set; }
    public string TaskName { get; private set; }
    public string TaskText { get; private set; }
    public short DifficultyLevel { get; private set; }
    public PublicationStatus PublicationStatus { get; private set; }

    public Topic Topic { get; private set; } = null!;
    public SqlQuery SqlQuery { get; private set; } = null!;

    private SqlTask(Guid id, Guid topicId, Guid sqlQueryId,
        string taskName, string taskText, short difficultyLevel,
        PublicationStatus publicationStatus) : base(id)
    {
        TopicId = topicId;
        SqlQueryId = sqlQueryId;
        TaskName = taskName;
        TaskText = taskText;
        DifficultyLevel = difficultyLevel;
        PublicationStatus = publicationStatus;
    }

    public static SqlTask Create(
        Guid topicId, Guid sqlQueryId,
        string taskName, string taskText, short difficultyLevel,
        Guid? id = null,
        PublicationStatus publicationStatus = PublicationStatus.Draft)
        => new(id ?? Guid.NewGuid(), topicId, sqlQueryId, taskName, taskText, difficultyLevel, publicationStatus);

    public void Update(
        string taskName,
        string taskText,
        short difficultyLevel,
        PublicationStatus? publicationStatus = null)
    {
        TaskName = taskName;
        TaskText = taskText;
        DifficultyLevel = difficultyLevel;
        if (publicationStatus.HasValue)
        {
            PublicationStatus = publicationStatus.Value;
        }
    }

    public void Publish()
    {
        PublicationStatus = PublicationStatus.Published;
    }

    public void ChangeTopic(Guid topicId)
    {
        TopicId = topicId;
    }
}
