using SQLModule.Domain.Common;
using SQLModule.Domain.Schema;

namespace SQLModule.Domain.Training;

/// <summary>Задание SQL-тренажёра.</summary>
public sealed class SqlTask : AuditableEntity
{
    public Guid TargetDbId { get; private set; }
    public Guid TopicId { get; private set; }
    public Guid SqlQueryId { get; private set; }
    public string TaskName { get; private set; }
    public string TaskText { get; private set; }
    public short DifficultyLevel { get; private set; }

    public TargetDb TargetDb { get; private set; } = null!;
    public Topic Topic { get; private set; } = null!;
    public SqlQuery SqlQuery { get; private set; } = null!;

    private SqlTask(Guid id, Guid targetDbId, Guid topicId, Guid sqlQueryId,
        string taskName, string taskText, short difficultyLevel) : base(id)
    {
        TargetDbId = targetDbId;
        TopicId = topicId;
        SqlQueryId = sqlQueryId;
        TaskName = taskName;
        TaskText = taskText;
        DifficultyLevel = difficultyLevel;
    }

    public static SqlTask Create(
        Guid targetDbId, Guid topicId, Guid sqlQueryId,
        string taskName, string taskText, short difficultyLevel,
        Guid? id = null)
        => new(id ?? Guid.NewGuid(), targetDbId, topicId, sqlQueryId, taskName, taskText, difficultyLevel);

    public void Update(string taskName, string taskText, short difficultyLevel)
    {
        TaskName = taskName;
        TaskText = taskText;
        DifficultyLevel = difficultyLevel;
    }
}
