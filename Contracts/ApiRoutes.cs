namespace Contracts;

/// <summary>
/// Пути к API
/// </summary>
public static class ApiRoutes
{
    /// <summary>
    /// Общий префикс Api v1
    /// </summary>
    public const string PrefixV1 = "api/v1";

    /// <summary>
    /// Маршруты ресурса Template
    /// </summary>
    public static class Template
    {
        /// <summary>
        /// Коллекция шаблонов (абсолютный путь, используется клиентом и сервером как база группы)
        /// </summary>
        public const string TemplateObjects = PrefixV1 + "/templates";

        /// <summary>
        /// Шаблон по идентификатору (абсолютный путь)
        /// </summary>
        public const string TemplateObject = TemplateObjects + "/{id}";

        /// <summary>
        /// Относительный маршрут коллекции внутри группы
        /// </summary>
        public const string CollectionRoute = "";

        /// <summary>
        /// Относительный маршрут единичного ресурса внутри группы
        /// </summary>
        public const string ByIdRoute = "{id}";

        /// <summary>
        /// Формирует абсолютный URL для конкретного шаблона
        /// </summary>
        public static string ForTemplateObject(Guid id)
            => ReplaceUrlSegment(TemplateObject, "id", id.ToString());

        /// <summary>
        /// Формирует абсолютный URL с параметрами пагинации
        /// </summary>
        public static string ForTemplateObjectPagination(int offset, int limit)
            => ReplaceUrlSegments(
                TemplateObjects + "?offset={offset}&limit={limit}",
                ("offset", offset.ToString()),
                ("limit", limit.ToString()));

        private static string ReplaceUrlSegment(string template, string name, string value)
        {
            var escapedUri = Uri.EscapeDataString(value);
            return template.Replace('{' + name + '}', escapedUri);
        }

        private static string ReplaceUrlSegments(string template, params (string Name, string Value)[] segments)
            => segments.Aggregate(template, (current, segment) => ReplaceUrlSegment(current, segment.Name, segment.Value));
    }
}
