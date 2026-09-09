namespace SQLModule.Contracts.Schema.SchemaBuilder;

/// <summary>Полное желаемое состояние схемы существующей учебной базы.</summary>
public sealed class SchemaUpsertRequest
{
    public string? Version { get; init; }
    public IReadOnlyList<SchemaTableDraft>? Tables { get; init; }
    public IReadOnlyList<SchemaRelationshipDraft>? Relationships { get; init; }
}

public sealed class SchemaTableDraft
{
    public Guid? Id { get; init; }
    public string? TempId { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public short? SortOrder { get; init; }
    public IReadOnlyList<SchemaColumnDraft>? Columns { get; init; }
}

public sealed class SchemaColumnDraft
{
    public Guid? Id { get; init; }
    public string? TempId { get; init; }
    public string? Name { get; init; }
    public Guid? PhysicalTypeId { get; init; }
    public bool? IsPrimaryKey { get; init; }
    public bool? IsRequired { get; init; }
    public short? SortOrder { get; init; }
    public IReadOnlyList<SchemaColumnParameterDraft>? Parameters { get; init; }
}

public sealed class SchemaColumnParameterDraft
{
    public Guid? ParameterDefinitionId { get; init; }
    public string? Value { get; init; }
}

public sealed class SchemaRelationshipDraft
{
    public Guid? Id { get; init; }
    public string? TempId { get; init; }
    public string? Name { get; init; }
    public string? SourceColumnRef { get; init; }
    public string? TargetColumnRef { get; init; }
    public string? DeleteRule { get; init; }
    public string? UpdateRule { get; init; }
}

public sealed class SchemaValidationResponse
{
    public required bool IsValid { get; init; }
    public bool RequiresConfirmation { get; init; }
    public string? NormalizedVersion { get; init; }
    public required IReadOnlyList<string> Warnings { get; init; }
    public IReadOnlyList<SchemaChangeResponse> Changes { get; init; } = [];
    public IReadOnlyList<SchemaChangeResponse> DestructiveChanges { get; init; } = [];
    public string? ConfirmationToken { get; init; }
    public string? DdlPreview { get; init; }
}

public sealed class SchemaChangeResponse
{
    public required string Kind { get; init; }
    public required string EntityType { get; init; }
    public Guid? EntityId { get; init; }
    public string? TempId { get; init; }
    public required string Path { get; init; }
    public required string Description { get; init; }
    public long? AffectedRows { get; init; }
    public required string Severity { get; init; }
    public string? Code { get; init; }
}
