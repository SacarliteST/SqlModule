namespace SQLModule.Contracts.Training.SqlTask;

/// <summary>Учебная база в деталях задания.</summary>
public sealed record TeacherTaskTargetDbResponse(
    Guid TargetDbId,
    string DbName,
    string DbmsName,
    IReadOnlyList<TeacherTaskTableResponse> Tables);
