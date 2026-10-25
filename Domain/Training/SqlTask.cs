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
    public Guid? ActiveValidationVersionId { get; private set; }
    public TaskVisibility Visibility { get; private set; }
    public long? BankNumber { get; private set; }
    public DateTimeOffset? BankPublishedAt { get; private set; }
    public string? BankHint { get; private set; }
    public bool ShowSolution { get; private set; }
    public string? SolutionExplanation { get; private set; }

    public Topic Topic { get; private set; } = null!;
    public SqlQuery SqlQuery { get; private set; } = null!;
    public TaskValidationConfiguration? ValidationConfiguration { get; private set; }
    public TaskValidationVersion? ActiveValidationVersion { get; private set; }

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
        Visibility = TaskVisibility.Control;
        ShowSolution = true;
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

    public void ActivateValidationVersion(Guid validationVersionId)
    {
        ActiveValidationVersionId = validationVersionId;
    }

    /// <summary>Сбрасывает активную версию оценки — нужно перед удалением версий задания (взаимные внешние ключи).</summary>
    public void ResetActiveValidationVersion()
    {
        ActiveValidationVersionId = null;
    }

    public void UpdateBankContent(string? bankHint, bool showSolution, string? solutionExplanation)
    {
        BankHint = NormalizeOptionalText(bankHint);
        ShowSolution = showSolution;
        SolutionExplanation = NormalizeOptionalText(solutionExplanation);
    }

    public void OpenInBank(long bankNumber, DateTimeOffset publishedAt)
    {
        if (BankNumber is null)
        {
            BankNumber = bankNumber;
            BankPublishedAt = publishedAt;
        }

        Visibility = TaskVisibility.Open;
    }

    public void CloseBank()
    {
        Visibility = TaskVisibility.Control;
    }

    private static string? NormalizeOptionalText(string? value) =>
        String.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
