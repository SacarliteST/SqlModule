using SQLModule.Domain.Common;

namespace SQLModule.Domain.Training;

/// <summary>Эталонный SQL-запрос для проверки решения пользователя.</summary>
public sealed class SqlQuery : AuditableEntity
{
    public Guid TargetDbId { get; private set; }
    public string QueryText { get; private set; }
    public bool StrictColumnOrder { get; private set; }
    public bool StrictRowOrder { get; private set; }

    /// <summary>Золотой результат в формате JSON (колонки + строки). Null до первого вычисления.</summary>
    public string? ExpectedResult { get; private set; }

    /// <summary>Задание, которому принадлежит эталонный запрос.</summary>
    public SqlTask? Task { get; private set; }

    private SqlQuery(Guid id, Guid targetDbId, string queryText, bool strictColumnOrder, bool strictRowOrder)
        : base(id)
    {
        TargetDbId = targetDbId;
        QueryText = queryText;
        StrictColumnOrder = strictColumnOrder;
        StrictRowOrder = strictRowOrder;
    }

    public static SqlQuery Create(
        string queryText,
        bool strictColumnOrder,
        bool strictRowOrder,
        Guid targetDbId,
        Guid? id = null)
        => new(id ?? Guid.NewGuid(), targetDbId, queryText, strictColumnOrder, strictRowOrder);

    public void Update(
        Guid targetDbId,
        string queryText,
        bool strictColumnOrder,
        bool strictRowOrder)
    {
        TargetDbId = targetDbId;
        QueryText = queryText;
        StrictColumnOrder = strictColumnOrder;
        StrictRowOrder = strictRowOrder;
    }

    public void SetExpectedResult(string json)
    {
        ExpectedResult = json;
    }
}
