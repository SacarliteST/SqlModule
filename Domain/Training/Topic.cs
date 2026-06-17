using SQLModule.Domain.Common;

namespace SQLModule.Domain.Training;

/// <summary>Тематический раздел для группировки заданий.</summary>
public sealed class Topic : AuditableEntity
{
    public string TopicName { get; private set; }
    public Guid? ParentTopicId { get; private set; }
    public Topic? ParentTopic { get; private set; }

    private readonly List<Topic> subTopics = [];
    public IReadOnlyCollection<Topic> SubTopics => subTopics.AsReadOnly();

    private readonly List<SqlTask> tasks = [];
    public IReadOnlyCollection<SqlTask> Tasks => tasks.AsReadOnly();

    private Topic(Guid id, string topicName, Guid? parentTopicId) : base(id)
    {
        TopicName = topicName;
        ParentTopicId = parentTopicId;
    }

    public static Topic Create(string topicName, Guid? parentTopicId = null, Guid? id = null)
        => new(id ?? Guid.NewGuid(), topicName, parentTopicId);

    public void Update(string topicName) => TopicName = topicName;

    public void Move(Guid? newParentTopicId) => ParentTopicId = newParentTopicId;
}
