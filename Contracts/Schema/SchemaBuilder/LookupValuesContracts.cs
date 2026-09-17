namespace SQLModule.Contracts.Schema.SchemaBuilder;

/// <summary>Параметры поиска значений связанной таблицы.</summary>
public sealed class LookupValuesRequest
{
    /// <summary>Колонка, значение которой будет сохранено во внешнем ключе.</summary>
    public Guid? ValueColumnId { get; init; }

    /// <summary>Необязательная колонка с понятным пользователю названием.</summary>
    public Guid? LabelColumnId { get; init; }

    /// <summary>Подстрока для регистронезависимого поиска по значению и названию.</summary>
    public string? Search { get; init; }

    /// <summary>Количество пропускаемых строк. По умолчанию 0.</summary>
    public int? Offset { get; init; }

    /// <summary>Размер страницы. По умолчанию 30.</summary>
    public int? Limit { get; init; }
}

/// <summary>Страница вариантов значения внешнего ключа.</summary>
public sealed class LookupValuesResponse
{
    public required int Count { get; init; }
    public required int Offset { get; init; }
    public required int Limit { get; init; }
    public required IReadOnlyList<LookupValueItemResponse> Items { get; init; }
}

/// <summary>Одна строка связанной таблицы, доступная для выбора.</summary>
public sealed class LookupValueItemResponse
{
    public required Guid RowId { get; init; }
    public required string Value { get; init; }
    public required string Label { get; init; }
}
