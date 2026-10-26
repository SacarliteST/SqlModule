namespace SQLModule.Contracts.Training.Validation;

/// <summary>Разрешённые преподавателем подсказки по validation version.</summary>
public sealed record StudentTaskHintsResponse(
    IReadOnlyList<HintGroup> Groups,
    IReadOnlyList<SqlConstruct> RequiredConstructs,
    IReadOnlyList<SqlConstruct> ForbiddenConstructs,
    IReadOnlyList<StudentHintTableResponse> RequiredTables,
    IReadOnlyList<StudentHintTableResponse> ForbiddenTables);
