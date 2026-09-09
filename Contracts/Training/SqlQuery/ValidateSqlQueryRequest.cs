namespace SQLModule.Contracts.Training.SqlQuery;

/// <summary>Запрос на проверку эталонного SQL без сохранения.</summary>
/// <param name="TargetDbId">Идентификатор учебной базы, на которой выполняется запрос.</param>
/// <param name="QueryText">Проверяемый текст SQL-запроса.</param>
public sealed record ValidateSqlQueryRequest(
    Guid TargetDbId,
    string QueryText);
