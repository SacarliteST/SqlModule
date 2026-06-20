namespace SQLModule.Contracts.DbmsCatalog.ParameterDefinition;

/// <summary>Запрос на создание определения параметра физического типа данных.</summary>
/// <param name="PhysicalTypeId">Идентификатор физического типа, которому принадлежит параметр.</param>
/// <param name="ParameterKey">Ключ параметра (не более 100 символов).</param>
/// <param name="DisplayName">Отображаемое название параметра (не более 200 символов).</param>
/// <param name="InputType">Тип поля ввода (не более 50 символов).</param>
/// <param name="DefaultValue">Значение по умолчанию (не более 500 символов).</param>
/// <param name="SortOrder">Порядок отображения (≥ 0).</param>
/// <param name="SqlFragment">Шаблон SQL-фрагмента с плейсхолдером {value} (не более 200 символов).</param>
/// <param name="IsRequired">Признак обязательности параметра.</param>
/// <param name="ValuePrefix">Префикс значения в SQL-выражении (не более 50 символов).</param>
/// <param name="ValueSuffix">Суффикс значения в SQL-выражении (не более 50 символов).</param>
/// <param name="Separator">Разделитель для списочных значений (не более 10 символов).</param>
public record CreateParameterDefinitionRequest(
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
    string? Separator);
