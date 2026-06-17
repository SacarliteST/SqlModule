namespace SQLModule.Domain.Common;

public abstract class BaseEntity : IBaseEntity
{
    public Guid Id { get; protected set; }
    protected BaseEntity(Guid id) => Id = id;
}
