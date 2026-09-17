namespace SQLModule.Contracts;

/// <summary>Пути к API.</summary>
public static class ApiRoutes
{
    /// <summary>Общий префикс Api v1.</summary>
    public const string PrefixV1 = "api/v1";

    /// <summary>Маршруты аутентификации собственного (standalone) контура SqlModule.</summary>
    public static class Auth
    {
        /// <summary>
        /// Standalone-вход: логин в IdentityService + обмен токена на audience SqlModule,
        /// без session_id.
        /// </summary>
        public const string Login = PrefixV1 + "/auth/login";
    }

    /// <summary>Маршруты интеграции с основной платформой.</summary>
    public static class ModuleIntegration
    {
        /// <summary>Каталог опубликованных заданий SQL-модуля.</summary>
        public const string TasksCatalog = PrefixV1 + "/module-integration/tasks-catalog";

        /// <summary>Платформенные сессии SQL-модуля.</summary>
        public const string Sessions = PrefixV1 + "/module-integration/sessions";

        /// <summary>Текущая платформенная сессия из JWT.</summary>
        public const string CurrentSession = Sessions + "/current";

        /// <summary>Финализация текущей платформенной сессии.</summary>
        public const string FinalizeCurrentSession = CurrentSession + "/finalize";
    }

    /// <summary>Маршруты контекста СУБД-справочника.</summary>
    public static class DbmsCatalog
    {
        /// <summary>Возможности проверки SQL для конкретной СУБД.</summary>
        public static class ValidationCapabilities
        {
            /// <summary>Возможности по идентификатору СУБД.</summary>
            public const string ByDbmsId = PrefixV1 + "/dbms/{dbmsId}/validation-capabilities";

            /// <inheritdoc cref="ForDbms"/>
            public static string ForDbms(Guid dbmsId) =>
                $"{PrefixV1}/dbms/{dbmsId}/validation-capabilities";
        }

        /// <summary>Справочник СУБД.</summary>
        public static class DbmsDictionaries
        {
            /// <summary>Коллекция.</summary>
            public const string Collection = PrefixV1 + "/dbms-dictionaries";

            /// <summary>Элемент по Id.</summary>
            public const string ById = Collection + "/{id}";

            /// <summary>Проверка Docker-конфигурации без сохранения.</summary>
            public const string Validate = Collection + "/validate";

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
        /// <summary>Построитель схемы (черновик с TempId).</summary>
        public static class SchemaBuilder
        {
            /// <summary>Коллекция схем.</summary>
            public const string Collection = PrefixV1 + "/schemas";

            /// <summary>Валидация черновика схемы в песочнице без сохранения.</summary>
            public const string Validate = PrefixV1 + "/schemas/validate";
        }

        /// <summary>Целевые БД.</summary>
        public static class TargetDbs
        {
            /// <summary>Коллекция.</summary>
            public const string Collection = PrefixV1 + "/target-dbs";

            /// <summary>Элемент по Id.</summary>
            public const string ById = Collection + "/{id}";

            /// <summary>Агрегированная схема учебной базы.</summary>
            public const string Schema = ById + "/schema";

            /// <summary>Проверка желаемой схемы существующей учебной базы.</summary>
            public const string ValidateSchema = Schema + "/validate";

            /// <summary>Строки конкретной таблицы учебной базы.</summary>
            public const string TableRows = ById + "/tables/{tableId}/rows";

            /// <summary>Значения таблицы для выбора внешнего ключа.</summary>
            public const string LookupValues = Collection + "/{targetDbId}/tables/{tableId}/lookup-values";

            /// <summary>Проверка пользовательского DDL без сохранения.</summary>
            public const string ValidateDdl = Collection + "/ddl/validate";

            /// <summary>Создание учебной базы из пользовательского DDL.</summary>
            public const string FromDdl = Collection + "/from-ddl";

            /// <inheritdoc cref="ForId"/>
            public static string ForId(Guid id) => $"{Collection}/{id}";

            /// <inheritdoc cref="ForSchema"/>
            public static string ForSchema(Guid id) => $"{Collection}/{id}/schema";

            /// <inheritdoc cref="ForValidateSchema"/>
            public static string ForValidateSchema(Guid id) => $"{Collection}/{id}/schema/validate";

            /// <inheritdoc cref="ForTableRows"/>
            public static string ForTableRows(Guid id, Guid tableId) => $"{Collection}/{id}/tables/{tableId}/rows";

            /// <inheritdoc cref="ForLookupValues"/>
            public static string ForLookupValues(Guid targetDbId, Guid tableId) =>
                $"{Collection}/{targetDbId}/tables/{tableId}/lookup-values";

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
        /// <summary>Безопасный публичный контур студента.</summary>
        public static class Student
        {
            public const string Topics = PrefixV1 + "/student/topics";
            public const string Tasks = PrefixV1 + "/student/tasks";
            public const string TaskById = Tasks + "/{taskId}";
            public const string TaskSchema = TaskById + "/schema";
            public const string TaskProgress = TaskById + "/progress";
            public const string TaskProgressRestart = TaskProgress + "/restart";
            public const string TaskProgressFinalize = TaskProgress + "/finalize";
            public const string Attempts = PrefixV1 + "/student/attempts";
            public const string AttemptById = Attempts + "/{attemptId}";

            public static string ForTask(Guid taskId) => $"{Tasks}/{taskId}";
            public static string ForTaskSchema(Guid taskId) => $"{ForTask(taskId)}/schema";
            public static string ForTaskProgress(Guid taskId) => $"{ForTask(taskId)}/progress";
            public static string ForTaskProgressRestart(Guid taskId) => $"{ForTaskProgress(taskId)}/restart";
            public static string ForTaskProgressFinalize(Guid taskId) => $"{ForTaskProgress(taskId)}/finalize";
            public static string ForAttempt(Guid attemptId) => $"{Attempts}/{attemptId}";
            public static string ForTasksPage(int offset, int limit) =>
                $"{Tasks}?offset={offset}&limit={limit}";
            public static string ForAttemptsPage(int offset, int limit) =>
                $"{Attempts}?offset={offset}&limit={limit}";
        }

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

            /// <summary>Проверка SQL-запроса без сохранения.</summary>
            public const string Validate = Collection + "/validate";

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

            /// <summary>Публикация задания.</summary>
            public const string Publish = ById + "/publish";

            /// <summary>Архивирование задания.</summary>
            public const string Archive = ById + "/archive";

            /// <summary>Эталонное решение текущего задания.</summary>
            public const string ReferenceQuery = ById + "/reference-query";

            /// <summary>Редактируемая конфигурация проверки задания.</summary>
            public const string Validation = Collection + "/{taskId}/validation";

            /// <summary>Предварительная проверка draft-конфигурации.</summary>
            public const string ValidationPreview = Validation + "/preview";

            /// <summary>Публикация immutable validation version.</summary>
            public const string ValidationPublish = Validation + "/publish";

            /// <inheritdoc cref="ForId"/>
            public static string ForId(Guid id) => $"{Collection}/{id}";

            /// <inheritdoc cref="ForPublish"/>
            public static string ForPublish(Guid id) => $"{Collection}/{id}/publish";

            /// <inheritdoc cref="ForArchive"/>
            public static string ForArchive(Guid id) => $"{Collection}/{id}/archive";

            /// <inheritdoc cref="ForReferenceQuery"/>
            public static string ForReferenceQuery(Guid id) => $"{Collection}/{id}/reference-query";

            /// <inheritdoc cref="ForValidation"/>
            public static string ForValidation(Guid id) => $"{Collection}/{id}/validation";

            /// <inheritdoc cref="ForValidationPreview"/>
            public static string ForValidationPreview(Guid id) => $"{ForValidation(id)}/preview";

            /// <inheritdoc cref="ForValidationPublish"/>
            public static string ForValidationPublish(Guid id) => $"{ForValidation(id)}/publish";

            /// <inheritdoc cref="ForPagination"/>
            public static string ForPagination(int offset, int limit) => $"{Collection}?offset={offset}&limit={limit}";
        }

        /// <summary>Преподавательские read-модели заданий.</summary>
        public static class TeacherTasks
        {
            /// <summary>Агрегированные детали задания.</summary>
            public const string Details = PrefixV1 + "/teacher/tasks/{taskId}/details";

            /// <inheritdoc cref="ForDetails"/>
            public static string ForDetails(Guid taskId) => $"{PrefixV1}/teacher/tasks/{taskId}/details";
        }

        /// <summary>Попытки выполнения заданий.</summary>
        public static class Attempts
        {
            /// <summary>Коллекция.</summary>
            public const string Collection = PrefixV1 + "/attempts";

            /// <summary>Элемент по Id.</summary>
            public const string ById = Collection + "/{id}";

            /// <summary>Справочники фильтров преподавательского журнала.</summary>
            public const string FilterOptions = Collection + "/filter-options";
            public const string StudentFilterOptions = FilterOptions + "/students";
            public const string TopicFilterOptions = FilterOptions + "/topics";
            public const string TaskFilterOptions = FilterOptions + "/tasks";

            /// <inheritdoc cref="ForId"/>
            public static string ForId(Guid id) => $"{Collection}/{id}";

            /// <inheritdoc cref="ForPagination"/>
            public static string ForPagination(int offset, int limit) => $"{Collection}?offset={offset}&limit={limit}";
        }
    }
}
