namespace SQLModule.Contracts.Training.SqlTask;

/// <summary>Эталонный запрос в деталях задания.</summary>
public sealed record TeacherTaskSqlQueryResponse(
    Guid SqlQueryId,
    string SqlText,
    bool IsRequiredColumnOrder,
    bool IsRequiredRowOrder);
