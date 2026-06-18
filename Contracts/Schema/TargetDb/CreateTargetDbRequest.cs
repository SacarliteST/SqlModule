namespace SQLModule.Contracts.Schema.TargetDb;

public record CreateTargetDbRequest(
    Guid DbmsId,
    string DbName,
    string? Description,
    bool IsReadOnly);
