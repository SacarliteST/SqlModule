namespace SQLModule.Contracts.Schema.TargetDb;

public record UpdateTargetDbRequest(
    string DbName,
    string? Description,
    bool IsReadOnly);
