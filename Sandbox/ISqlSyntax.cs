namespace SQLModule.Sandbox;

/// <summary>Синтаксические помощники для генерации SQL-текста конкретного диалекта.</summary>
public interface ISqlSyntax
{
    /// <summary>Системное имя диалекта (например, «postgres», «mysql»).</summary>
    string SystemName { get; }

    /// <summary>Оборачивает идентификатор в диалектные кавычки и экранирует их внутри имени.</summary>
    string QuoteIdentifier(string name);

    /// <summary>
    /// Форматирует значение ячейки в SQL-литерал:
    /// <c>null</c> → <c>NULL</c>, числа — без кавычек, строки — в одинарных кавычках с экранированием.
    /// </summary>
    string FormatValue(string? value);
}
