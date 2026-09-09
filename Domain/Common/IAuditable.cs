namespace SQLModule.Domain.Common;

public interface IAuditable
{
    Guid CreatedById { get; }
    string? CreatedByName { get; }
    DateTimeOffset CreatedAt { get; }
    Guid UpdatedById { get; }
    string? UpdatedByName { get; }
    DateTimeOffset UpdatedAt { get; }
}
