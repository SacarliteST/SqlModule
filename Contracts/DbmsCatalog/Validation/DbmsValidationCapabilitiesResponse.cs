using SQLModule.Domain.Training;

namespace SQLModule.Contracts.DbmsCatalog.Validation;

/// <summary>Возможности проверки SQL для конкретной СУБД.</summary>
public sealed record DbmsValidationCapabilitiesResponse(
    Guid DbmsId,
    IReadOnlyList<ValidationCheckKind> SupportedCheckKinds,
    IReadOnlyList<SqlConstruct> SupportedConstructs,
    IReadOnlyList<HintGroup> SupportedHintGroups,
    int MaxAttemptsLimit,
    string AnalyzerVersion);
