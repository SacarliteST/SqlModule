namespace SQLModule.Contracts;

/// <summary>Пути к API.</summary>
public static class ApiRoutes
{
    /// <summary>Общий префикс Api v1.</summary>
    public const string PrefixV1 = "api/v1";

    /// <summary>Маршруты контекста СУБД-справочника.</summary>
    public static class DbmsCatalog
    {
        /// <summary>Справочник СУБД.</summary>
        public static class DbmsDictionaries
        {
            /// <summary>Коллекция.</summary>
            public const string Collection = PrefixV1 + "/dbms-dictionaries";

            /// <summary>Элемент по Id.</summary>
            public const string ById = Collection + "/{id}";

            /// <inheritdoc cref="ForId"/>
            public static string ForId(Guid id) => $"{Collection}/{id}";

            /// <inheritdoc cref="ForPagination"/>
            public static string ForPagination(int offset, int limit) => $"{Collection}?offset={offset}&limit={limit}";
        }

        /// <summary>Физические типы данных.</summary>
        public static class PhysicalTypes
        {
            /// <summary>Коллекция.</summary>
            public const string Collection = PrefixV1 + "/physical-types";

            /// <summary>Элемент по Id.</summary>
            public const string ById = Collection + "/{id}";

            /// <inheritdoc cref="ForId"/>
            public static string ForId(Guid id) => $"{Collection}/{id}";

            /// <inheritdoc cref="ForPagination"/>
            public static string ForPagination(int offset, int limit) => $"{Collection}?offset={offset}&limit={limit}";
        }

        /// <summary>Определения параметров физических типов.</summary>
        public static class ParameterDefinitions
        {
            /// <summary>Коллекция.</summary>
            public const string Collection = PrefixV1 + "/parameter-definitions";

            /// <summary>Элемент по Id.</summary>
            public const string ById = Collection + "/{id}";

            /// <inheritdoc cref="ForId"/>
            public static string ForId(Guid id) => $"{Collection}/{id}";

            /// <inheritdoc cref="ForPagination"/>
            public static string ForPagination(int offset, int limit) => $"{Collection}?offset={offset}&limit={limit}";
        }
    }

    /// <summary>Маршруты контекста схемы данных.</summary>
    public static class Schema
    {
        /// <summary>Целевые БД.</summary>
        public static class TargetDbs
        {
            /// <summary>Коллекция.</summary>
            public const string Collection = PrefixV1 + "/target-dbs";

            /// <summary>Элемент по Id.</summary>
            public const string ById = Collection + "/{id}";

            /// <inheritdoc cref="ForId"/>
            public static string ForId(Guid id) => $"{Collection}/{id}";

            /// <inheritdoc cref="ForPagination"/>
            public static string ForPagination(int offset, int limit) => $"{Collection}?offset={offset}&limit={limit}";
        }

        /// <summary>Мета-таблицы.</summary>
        public static class MetaTables
        {
            /// <summary>Коллекция.</summary>
            public const string Collection = PrefixV1 + "/meta-tables";

            /// <summary>Элемент по Id.</summary>
            public const string ById = Collection + "/{id}";

            /// <inheritdoc cref="ForId"/>
            public static string ForId(Guid id) => $"{Collection}/{id}";

            /// <inheritdoc cref="ForPagination"/>
            public static string ForPagination(int offset, int limit) => $"{Collection}?offset={offset}&limit={limit}";
        }

        /// <summary>Мета-атрибуты.</summary>
        public static class MetaAttributes
        {
            /// <summary>Коллекция.</summary>
            public const string Collection = PrefixV1 + "/meta-attributes";

            /// <summary>Элемент по Id.</summary>
            public const string ById = Collection + "/{id}";

            /// <inheritdoc cref="ForId"/>
            public static string ForId(Guid id) => $"{Collection}/{id}";

            /// <inheritdoc cref="ForPagination"/>
            public static string ForPagination(int offset, int limit) => $"{Collection}?offset={offset}&limit={limit}";
        }

        /// <summary>Связи между атрибутами.</summary>
        public static class MetaRelationships
        {
            /// <summary>Коллекция.</summary>
            public const string Collection = PrefixV1 + "/meta-relationships";

            /// <summary>Элемент по Id.</summary>
            public const string ById = Collection + "/{id}";

            /// <inheritdoc cref="ForId"/>
            public static string ForId(Guid id) => $"{Collection}/{id}";

            /// <inheritdoc cref="ForPagination"/>
            public static string ForPagination(int offset, int limit) => $"{Collection}?offset={offset}&limit={limit}";
        }

        /// <summary>Значения параметров атрибутов.</summary>
        public static class AttributeParameterValues
        {
            /// <summary>Коллекция.</summary>
            public const string Collection = PrefixV1 + "/attribute-parameter-values";

            /// <summary>Элемент по Id.</summary>
            public const string ById = Collection + "/{id}";

            /// <inheritdoc cref="ForId"/>
            public static string ForId(Guid id) => $"{Collection}/{id}";

            /// <inheritdoc cref="ForPagination"/>
            public static string ForPagination(int offset, int limit) => $"{Collection}?offset={offset}&limit={limit}";
        }

        /// <summary>Строки данных (EAV).</summary>
        public static class DataRecords
        {
            /// <summary>Коллекция.</summary>
            public const string Collection = PrefixV1 + "/data-records";

            /// <summary>Элемент по Id.</summary>
            public const string ById = Collection + "/{id}";

            /// <inheritdoc cref="ForId"/>
            public static string ForId(Guid id) => $"{Collection}/{id}";

            /// <inheritdoc cref="ForPagination"/>
            public static string ForPagination(int offset, int limit) => $"{Collection}?offset={offset}&limit={limit}";
        }

        /// <summary>Значения ячеек (EAV).</summary>
        public static class CellValues
        {
            /// <summary>Коллекция.</summary>
            public const string Collection = PrefixV1 + "/cell-values";

            /// <summary>Элемент по Id.</summary>
            public const string ById = Collection + "/{id}";

            /// <inheritdoc cref="ForId"/>
            public static string ForId(Guid id) => $"{Collection}/{id}";

            /// <inheritdoc cref="ForPagination"/>
            public static string ForPagination(int offset, int limit) => $"{Collection}?offset={offset}&limit={limit}";
        }
    }

    /// <summary>Маршруты контекста тренажёра.</summary>
    public static class Training
    {
        /// <summary>Темы.</summary>
        public static class Topics
        {
            /// <summary>Коллекция.</summary>
            public const string Collection = PrefixV1 + "/topics";

            /// <summary>Элемент по Id.</summary>
            public const string ById = Collection + "/{id}";

            /// <summary>Смена родительской темы.</summary>
            public const string Parent = ById + "/parent";

            /// <inheritdoc cref="ForId"/>
            public static string ForId(Guid id) => $"{Collection}/{id}";

            /// <inheritdoc cref="ForParent"/>
            public static string ForParent(Guid id) => $"{Collection}/{id}/parent";

            /// <inheritdoc cref="ForPagination"/>
            public static string ForPagination(int offset, int limit) => $"{Collection}?offset={offset}&limit={limit}";
        }

        /// <summary>SQL-запросы.</summary>
        public static class SqlQueries
        {
            /// <summary>Коллекция.</summary>
            public const string Collection = PrefixV1 + "/sql-queries";

            /// <summary>Элемент по Id.</summary>
            public const string ById = Collection + "/{id}";

            /// <inheritdoc cref="ForId"/>
            public static string ForId(Guid id) => $"{Collection}/{id}";

            /// <inheritdoc cref="ForPagination"/>
            public static string ForPagination(int offset, int limit) => $"{Collection}?offset={offset}&limit={limit}";
        }

        /// <summary>SQL-задания.</summary>
        public static class SqlTasks
        {
            /// <summary>Коллекция.</summary>
            public const string Collection = PrefixV1 + "/sql-tasks";

            /// <summary>Элемент по Id.</summary>
            public const string ById = Collection + "/{id}";

            /// <inheritdoc cref="ForId"/>
            public static string ForId(Guid id) => $"{Collection}/{id}";

            /// <inheritdoc cref="ForPagination"/>
            public static string ForPagination(int offset, int limit) => $"{Collection}?offset={offset}&limit={limit}";
        }

        /// <summary>Попытки выполнения заданий.</summary>
        public static class Attempts
        {
            /// <summary>Коллекция.</summary>
            public const string Collection = PrefixV1 + "/attempts";

            /// <summary>Элемент по Id.</summary>
            public const string ById = Collection + "/{id}";

            /// <inheritdoc cref="ForId"/>
            public static string ForId(Guid id) => $"{Collection}/{id}";

            /// <inheritdoc cref="ForPagination"/>
            public static string ForPagination(int offset, int limit) => $"{Collection}?offset={offset}&limit={limit}";
        }
    }
}
