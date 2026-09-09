namespace SQLModule.Contracts.Schema.SchemaBuilder;

public sealed class ValidateTargetDbDdlRequest
{
    public Guid? DbmsId { get; init; }
    public string? DbName { get; init; }
    public string? DdlScript { get; init; }
}

public sealed class CreateTargetDbFromDdlRequest
{
    public Guid? DbmsId { get; init; }
    public string? DbName { get; init; }
    public string? Description { get; init; }
    public bool? IsReadOnly { get; init; }
    public string? DdlScript { get; init; }
}

public sealed record ValidateTargetDbDdlResponse(
    bool IsValid,
    IReadOnlyList<string> Warnings,
    int DetectedTables,
    int DetectedRelationships);

public sealed record CreateTargetDbFromDdlResponse(
    Guid TargetDbId,
    TargetDbSchemaResponse Schema);
