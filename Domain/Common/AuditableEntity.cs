namespace SQLModule.Domain.Common;

public abstract class AuditableEntity : BaseEntity, IAuditable
{
    public Guid CreatedById { get; private set; }
    public string? CreatedByName { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid UpdatedById { get; private set; }
    public string? UpdatedByName { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    protected AuditableEntity(Guid id) : base(id) { }

    public void SetCreated(Guid userId, string? userName, DateTimeOffset at)
    {
        CreatedById = userId;
        CreatedByName = NormalizeName(userName, userId);
        CreatedAt = at;
        SetUpdated(userId, userName, at);
    }

    public void SetUpdated(Guid userId, string? userName, DateTimeOffset at)
    {
        UpdatedById = userId;
        UpdatedByName = NormalizeName(userName, userId);
        UpdatedAt = at;
    }

    private static string NormalizeName(string? userName, Guid userId) =>
        String.IsNullOrWhiteSpace(userName) ? userId.ToString() : userName.Trim();
}
