namespace SQLModule.Contracts.Schema.SchemaBuilder;

/// <summary>Черновик колонки с временным идентификатором.</summary>
/// <param name="TempId">Временный строковый идентификатор (уникальный внутри запроса).</param>
/// <param name="Name">Имя колонки в целевой БД.</param>
/// <param name="PhysicalTypeId">Идентификатор физического типа данных.</param>
/// <param name="IsPrimaryKey">Признак первичного ключа.</param>
/// <param name="IsRequired">Признак обязательности (NOT NULL).</param>
/// <param name="SortOrder">Порядок колонки в таблице (≥ 0).</param>
/// <param name="Parameters">Значения параметров физического типа (например, длина для VARCHAR).</param>
public record ColumnDraft(
    string TempId,
    string Name,
    Guid PhysicalTypeId,
    bool IsPrimaryKey,
    bool IsRequired,
    short SortOrder,
    IReadOnlyList<ColumnParameterDraft> Parameters);
