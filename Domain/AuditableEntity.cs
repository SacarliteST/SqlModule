namespace SQLModule.Domain;

public abstract class AuditableEntity : Entity, IAuditable
{
    public Guid CreatedById { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid UpdatedById { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    protected AuditableEntity(Guid id) : base(id) { }

    public void SetCreated(Guid userId, DateTimeOffset at)
    {
        CreatedById = userId;
        CreatedAt = at;
        SetUpdated(userId, at);
    }

    public void SetUpdated(Guid userId, DateTimeOffset at)
    {
        UpdatedById = userId;
        UpdatedAt = at;
    }
}
