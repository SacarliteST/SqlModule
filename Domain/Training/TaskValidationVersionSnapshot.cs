namespace SQLModule.Domain.Training;

/// <summary>Канонический материал immutable validation version.</summary>
public sealed record TaskValidationVersionSnapshot(
    string SchemaJson,
    string DatasetJson,
    string ReferenceQueryJson,
    string ExpectedResultJson,
    string ConfigurationJson,
    string AnalyzerVersion);
