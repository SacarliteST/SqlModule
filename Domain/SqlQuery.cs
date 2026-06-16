namespace SQLModule.Domain;

/// <summary>Эталонный SQL-запрос для проверки решения пользователя.</summary>
public sealed class SqlQuery : AuditableEntity
{
    public string QueryText { get; private set; }
    public bool StrictColumnOrder { get; private set; }
    public bool StrictRowOrder { get; private set; }

    private SqlQuery(Guid id, string queryText, bool strictColumnOrder, bool strictRowOrder) : base(id)
    {
        QueryText = queryText;
        StrictColumnOrder = strictColumnOrder;
        StrictRowOrder = strictRowOrder;
    }

    public static SqlQuery Create(string queryText, bool strictColumnOrder, bool strictRowOrder, Guid? id = null)
        => new(id ?? Guid.NewGuid(), queryText, strictColumnOrder, strictRowOrder);

    public void Update(string queryText, bool strictColumnOrder, bool strictRowOrder)
    {
        QueryText = queryText;
        StrictColumnOrder = strictColumnOrder;
        StrictRowOrder = strictRowOrder;
    }
}
