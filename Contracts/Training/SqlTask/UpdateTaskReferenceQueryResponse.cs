namespace SQLModule.Contracts.Training.SqlTask;

/// <summary>Актуальное состояние эталонного решения после обновления.</summary>
/// <param name="SqlText">Сохранённый текст эталонного SQL-запроса.</param>
/// <param name="IsRequiredColumnOrder">Требуется ли совпадение порядка колонок.</param>
/// <param name="IsRequiredRowOrder">Требуется ли совпадение порядка строк.</param>
/// <param name="TargetDb">Учебная база, в которой проверен эталон.</param>
public sealed record UpdateTaskReferenceQueryResponse(
    string SqlText,
    bool IsRequiredColumnOrder,
    bool IsRequiredRowOrder,
    UpdateTaskReferenceTargetDbResponse TargetDb);

/// <summary>Учебная база обновлённого эталонного решения.</summary>
/// <param name="TargetDbId">Идентификатор учебной базы.</param>
/// <param name="DbName">Название учебной базы.</param>
/// <param name="DbmsName">Название СУБД.</param>
public sealed record UpdateTaskReferenceTargetDbResponse(
    Guid TargetDbId,
    string DbName,
    string DbmsName);
