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

    public Topic Topic { get; private set; } = null!;
    public SqlQuery SqlQuery { get; private set; } = null!;

    private SqlTask(Guid id, Guid topicId, Guid sqlQueryId,
        string taskName, string taskText, short difficultyLevel) : base(id)
    {
        TopicId = topicId;
        SqlQueryId = sqlQueryId;
        TaskName = taskName;
        TaskText = taskText;
        DifficultyLevel = difficultyLevel;
    }

    public static SqlTask Create(
        Guid topicId, Guid sqlQueryId,
        string taskName, string taskText, short difficultyLevel,
        Guid? id = null)
        => new(id ?? Guid.NewGuid(), topicId, sqlQueryId, taskName, taskText, difficultyLevel);

    public void Update(string taskName, string taskText, short difficultyLevel)
    {
        TaskName = taskName;
        TaskText = taskText;
        DifficultyLevel = difficultyLevel;
    }
}
