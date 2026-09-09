namespace SQLModule.Web.Common.Isolated;

internal sealed class CourseSeedDocument
{
    public int FormatVersion { get; init; }
    public required CourseSeedCourse Course { get; init; }
    public required CourseSeedDbms Dbms { get; init; }
    public required List<CourseSeedPhysicalType> PhysicalTypes { get; init; }
    public required List<CourseSeedDatabase> Databases { get; init; }
    public required List<CourseSeedTopic> Topics { get; init; }
    public required List<CourseSeedTask> Tasks { get; init; }
}

internal sealed class CourseSeedCourse
{
    public Guid Id { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
}

internal sealed class CourseSeedDbms
{
    public Guid Id { get; init; }
    public required string DbmsName { get; init; }
    public required string DbmsSystemName { get; init; }
    public required string DockerImage { get; init; }
    public int DefaultPort { get; init; }
    public required string EnvUserKey { get; init; }
    public required string EnvPasswordKey { get; init; }
    public required string EnvDatabaseKey { get; init; }
    public string? ExtraEnvConfig { get; init; }
    public required string DefaultDatabase { get; init; }
    public required string DefaultUsername { get; init; }
    public required string DefaultPassword { get; init; }
}

internal sealed class CourseSeedPhysicalType
{
    public Guid Id { get; init; }
    public required string TypeName { get; init; }
    public required List<CourseSeedParameterDefinition> Parameters { get; init; }
}

internal sealed class CourseSeedParameterDefinition
{
    public Guid Id { get; init; }
    public required string Key { get; init; }
    public required string DisplayName { get; init; }
    public required string InputType { get; init; }
    public string? DefaultValue { get; init; }
    public short SortOrder { get; init; }
    public required string SqlFragment { get; init; }
    public bool IsRequired { get; init; } = true;
    public string? ValuePrefix { get; init; }
    public string? ValueSuffix { get; init; }
    public string? Separator { get; init; }
}

internal sealed class CourseSeedDatabase
{
    public Guid Id { get; init; }
    public Guid DbmsId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public bool IsReadOnly { get; init; }
    public required List<CourseSeedTable> Tables { get; init; }
    public required List<CourseSeedRelationship> Relationships { get; init; }
}

internal sealed class CourseSeedTable
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public short SortOrder { get; init; }
    public required List<CourseSeedColumn> Columns { get; init; }
    public required List<CourseSeedRow> Rows { get; init; }
}

internal sealed class CourseSeedColumn
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public Guid PhysicalTypeId { get; init; }
    public bool IsPrimaryKey { get; init; }
    public bool IsRequired { get; init; }
    public short SortOrder { get; init; }
    public required Dictionary<string, string> Parameters { get; init; }
}

internal sealed class CourseSeedRow
{
    public Guid Id { get; init; }
    public int? SortOrder { get; init; }
    public required Dictionary<string, string?> Values { get; init; }
}

internal sealed class CourseSeedRelationship
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public Guid FromTableId { get; init; }
    public Guid ToTableId { get; init; }
    public required List<CourseSeedColumnPair> ColumnPairs { get; init; }
    public string? OnDelete { get; init; }
    public string? OnUpdate { get; init; }
}

internal sealed class CourseSeedColumnPair
{
    public Guid FromColumnId { get; init; }
    public Guid ToColumnId { get; init; }
}

internal sealed class CourseSeedTopic
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public Guid? ParentTopicId { get; init; }
}

internal sealed class CourseSeedTask
{
    public Guid Id { get; init; }
    public Guid TopicId { get; init; }
    public Guid TargetDatabaseId { get; init; }
    public Guid ReferenceQueryId { get; init; }
    public required string Name { get; init; }
    public required string Text { get; init; }
    public short DifficultyLevel { get; init; }
    public required string PublicationStatus { get; init; }
    public required CourseSeedReferenceQuery ReferenceQuery { get; init; }
}

internal sealed class CourseSeedReferenceQuery
{
    public required string SqlText { get; init; }
    public bool StrictColumnOrder { get; init; }
    public bool StrictRowOrder { get; init; }
    public required CourseSeedExpectedResult ExpectedResult { get; init; }
}

internal sealed class CourseSeedExpectedResult
{
    public required List<string> Columns { get; init; }
    public required List<List<string?>> Rows { get; init; }
}
