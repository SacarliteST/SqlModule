namespace SQLModule.Contracts.Training.Student;

/// <summary>Публичные ограничения выполнения SQL.</summary>
public sealed record StudentExecutionLimitsResponse(int TimeoutSeconds, int MaxRows, int MaxSqlLength);
