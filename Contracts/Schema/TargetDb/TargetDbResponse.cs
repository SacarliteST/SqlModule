namespace SQLModule.Contracts.Schema.TargetDb;

public record TargetDbResponse(
    Guid Id,
    Guid DbmsId,
    string DbName,
    string? Description,
    bool IsReadOnly,
    Guid CreatedById,
    DateTimeOffset CreatedAt,
    Guid UpdatedById,
    DateTimeOffset UpdatedAt);
