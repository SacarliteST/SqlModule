namespace SQLModule.Contracts.DbmsCatalog.ParameterDefinition;

/// <summary>Запрос на обновление определения параметра физического типа данных.</summary>
/// <param name="ParameterKey">Новый ключ параметра (не более 100 символов).</param>
/// <param name="DisplayName">Новое отображаемое название параметра (не более 200 символов).</param>
/// <param name="InputType">Новый тип поля ввода (не более 50 символов).</param>
/// <param name="DefaultValue">Новое значение по умолчанию (не более 500 символов).</param>
/// <param name="SortOrder">Новый порядок отображения (≥ 0).</param>
/// <param name="SqlFragment">Новый шаблон SQL-фрагмента (не более 200 символов).</param>
/// <param name="IsRequired">Новый признак обязательности параметра.</param>
/// <param name="ValuePrefix">Новый префикс значения (не более 50 символов).</param>
/// <param name="ValueSuffix">Новый суффикс значения (не более 50 символов).</param>
/// <param name="Separator">Новый разделитель (не более 10 символов).</param>
public record UpdateParameterDefinitionRequest(
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
