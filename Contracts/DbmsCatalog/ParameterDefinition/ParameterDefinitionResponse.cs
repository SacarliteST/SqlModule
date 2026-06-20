namespace SQLModule.Contracts.DbmsCatalog.ParameterDefinition;

/// <summary>Ответ с данными определения параметра физического типа данных.</summary>
/// <param name="Id">Идентификатор определения параметра.</param>
/// <param name="PhysicalTypeId">Идентификатор физического типа, которому принадлежит параметр.</param>
/// <param name="ParameterKey">Ключ параметра.</param>
/// <param name="DisplayName">Отображаемое название параметра.</param>
/// <param name="InputType">Тип поля ввода.</param>
/// <param name="DefaultValue">Значение по умолчанию.</param>
/// <param name="SortOrder">Порядок отображения.</param>
/// <param name="SqlFragment">Шаблон SQL-фрагмента с плейсхолдером {value}.</param>
/// <param name="IsRequired">Признак обязательности параметра.</param>
/// <param name="ValuePrefix">Префикс значения в SQL-выражении.</param>
/// <param name="ValueSuffix">Суффикс значения в SQL-выражении.</param>
/// <param name="Separator">Разделитель для списочных значений.</param>
/// <param name="CreatedById">Идентификатор пользователя, создавшего запись.</param>
/// <param name="CreatedAt">Дата и время создания записи.</param>
/// <param name="UpdatedById">Идентификатор пользователя, последним обновившего запись.</param>
/// <param name="UpdatedAt">Дата и время последнего обновления записи.</param>
public record ParameterDefinitionResponse(
    Guid Id,
    Guid PhysicalTypeId,
    string ParameterKey,
    string DisplayName,
    string InputType,
    string? DefaultValue,
    short SortOrder,
    string SqlFragment,
    bool IsRequired,
    string? ValuePrefix,
    string? ValueSuffix,
    string? Separator,
    Guid CreatedById,
    DateTimeOffset CreatedAt,
    Guid UpdatedById,
    DateTimeOffset UpdatedAt);
