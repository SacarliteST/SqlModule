using SQLModule.Domain.Common;

namespace SQLModule.Domain.Training;

/// <summary>Тематический раздел для группировки заданий.</summary>
public sealed class Topic : AuditableEntity
{
    public string TopicName { get; private set; }
    public Guid? ParentTopicId { get; private set; }
    public string? Description { get; private set; }
    public Topic? ParentTopic { get; private set; }

    private readonly List<Topic> subTopics = [];
    public IReadOnlyCollection<Topic> SubTopics => subTopics.AsReadOnly();

    private readonly List<SqlTask> tasks = [];
    public IReadOnlyCollection<SqlTask> Tasks => tasks.AsReadOnly();

    private Topic(Guid id, string topicName, Guid? parentTopicId, string? description) : base(id)
    {
        TopicName = topicName;
        ParentTopicId = parentTopicId;
        Description = description;
    }

    public static Topic Create(string topicName, Guid? parentTopicId = null, Guid? id = null, string? description = null)
        => new(id ?? Guid.NewGuid(), topicName, parentTopicId, description);

    public void Update(string topicName, string? description = null)
    {
        TopicName = topicName;
        Description = description;
    }

    public void Move(Guid? newParentTopicId) => ParentTopicId = newParentTopicId;
}
