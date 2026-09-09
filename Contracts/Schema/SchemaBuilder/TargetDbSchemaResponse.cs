namespace SQLModule.Contracts.Schema.SchemaBuilder;

/// <summary>Согласованный снимок логической схемы учебной базы.</summary>
public sealed class TargetDbSchemaResponse
{
    public required Guid TargetDbId { get; init; }
    public required Guid DbmsId { get; init; }
    public required string DbName { get; init; }
    public required string Version { get; init; }
    public required SchemaLifecycleState State { get; init; }
    public required SchemaCapabilitiesResponse Capabilities { get; init; }
    public required IReadOnlyList<SchemaTableResponse> Tables { get; init; }
    public required IReadOnlyList<SchemaRelationshipResponse> Relationships { get; init; }
}

public enum SchemaLifecycleState { Draft, Ready }

public sealed class SchemaCapabilitiesResponse
{
    public required bool CanEditSchema { get; init; }
    public required bool CanValidateSchema { get; init; }
    public required bool CanApplySchema { get; init; }
    public required bool CanEditData { get; init; }
    public required bool CanDeleteTargetDb { get; init; }
    public string? SchemaEditBlockReason { get; init; }
    public string? DataEditBlockReason { get; init; }
    public string? DeleteTargetDbBlockReason { get; init; }
}

public sealed class SchemaTableResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required short SortOrder { get; init; }
    public required IReadOnlyList<SchemaColumnResponse> Columns { get; init; }
}

public sealed class SchemaColumnResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required Guid PhysicalTypeId { get; init; }
    public required string PhysicalTypeName { get; init; }
    public required bool IsPrimaryKey { get; init; }
    public required bool IsRequired { get; init; }
    public required short SortOrder { get; init; }
    public required IReadOnlyList<SchemaParameterResponse> Parameters { get; init; }
}

public sealed class SchemaParameterResponse
{
    public required Guid ParameterDefinitionId { get; init; }
    public required string ParameterKey { get; init; }
    public required string DisplayName { get; init; }
    public required string InputType { get; init; }
    public string? Value { get; init; }
    public required bool IsRequired { get; init; }
}

public sealed class SchemaRelationshipResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required Guid SourceColumnId { get; init; }
    public required Guid TargetColumnId { get; init; }
    public string? DeleteRule { get; init; }
    public string? UpdateRule { get; init; }
}
