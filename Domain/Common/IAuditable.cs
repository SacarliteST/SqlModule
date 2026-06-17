namespace SQLModule.Domain.Common;

public interface IAuditable
{
    Guid CreatedById { get; }
    DateTimeOffset CreatedAt { get; }
    Guid UpdatedById { get; }
    DateTimeOffset UpdatedAt { get; }
}
