using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SQLModule.Data.Core;
using SQLModule.Domain.DbmsCatalog;
using SQLModule.Domain.Schema;
using SQLModule.Domain.Training;
using SQLModule.Sandbox;
using SQLModule.Web.Common.Sandbox;

namespace SQLModule.Web.Common.Isolated;

/// <summary>Идемпотентно импортирует описанный JSON учебный курс в доменную модель.</summary>
internal sealed class CourseDataSeeder(
    AppDbContext db,
    IOptions<CourseSeedOptions> options,
    IHostEnvironment environment)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var settings = options.Value;
        if (!settings.Enabled)
        {
            return;
        }

        var filePath = Path.IsPathRooted(settings.FilePath)
            ? settings.FilePath
            : Path.GetFullPath(Path.Combine(environment.ContentRootPath, settings.FilePath));
        if (!File.Exists(filePath))
        {
            throw new InvalidOperationException(
                $"CourseSeed:FilePath указывает на отсутствующий файл: {filePath}");
        }

        CourseSeedDocument document;
        try
        {
            await using var stream = File.OpenRead(filePath);
            document = await JsonSerializer.DeserializeAsync<CourseSeedDocument>(stream, JsonOptions, ct)
                       ?? throw new InvalidOperationException("Файл курса содержит пустой JSON.");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                $"Не удалось прочитать JSON курса из CourseSeed:FilePath: {exception.Message}",
                exception);
        }

        Validate(document);
        await ImportAsync(document, ct);
    }

    private async Task ImportAsync(CourseSeedDocument document, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct);

        var types = document.PhysicalTypes;
        var parameterDefinitions = types.SelectMany(type => type.Parameters).ToList();
        var databases = document.Databases;
        var tables = databases.SelectMany(database => database.Tables).ToList();
        var columns = tables.SelectMany(table => table.Columns).ToList();
        var rows = tables.SelectMany(table => table.Rows).ToList();
        var relationshipPairs = databases
            .SelectMany(database => database.Relationships)
            .SelectMany(ExpandRelationship)
            .ToList();
        var parameterValues = BuildParameterValues(columns, types);
        var cells = BuildCells(tables);

        var dbms = await db.DbmsDictionaries.SingleOrDefaultAsync(
            value => value.Id == document.Dbms.Id,
            ct);
        var existingTypes = await db.PhysicalTypes
            .Where(value => types.Select(item => item.Id).Contains(value.Id))
            .ToDictionaryAsync(value => value.Id, ct);
        var existingDefinitions = await db.ParameterDefinitions
            .Where(value => parameterDefinitions.Select(item => item.Id).Contains(value.Id))
            .ToDictionaryAsync(value => value.Id, ct);
        var existingDatabases = await db.TargetDbs
            .Where(value => databases.Select(item => item.Id).Contains(value.Id))
            .ToDictionaryAsync(value => value.Id, ct);
        var existingTables = await db.MetaTables
            .Where(value => tables.Select(item => item.Id).Contains(value.Id))
            .ToDictionaryAsync(value => value.Id, ct);
        var existingColumns = await db.MetaAttributes
            .Where(value => columns.Select(item => item.Id).Contains(value.Id))
            .ToDictionaryAsync(value => value.Id, ct);
        var existingRows = await db.DataRecords
            .Where(value => rows.Select(item => item.Id).Contains(value.Id))
            .ToDictionaryAsync(value => value.Id, ct);
        var existingCells = await db.CellValues
            .Where(value => cells.Select(item => item.Id).Contains(value.Id))
            .ToDictionaryAsync(value => value.Id, ct);
        var existingParameterValues = await db.AttributeParameterValues
            .Where(value => parameterValues.Select(item => item.Id).Contains(value.Id))
            .ToDictionaryAsync(value => value.Id, ct);
        var existingRelationships = await db.MetaRelationships
            .Where(value => relationshipPairs.Select(item => item.Id).Contains(value.Id))
            .ToDictionaryAsync(value => value.Id, ct);
        var existingTopics = await db.Topics
            .Where(value => document.Topics.Select(item => item.Id).Contains(value.Id))
            .ToDictionaryAsync(value => value.Id, ct);
        var existingQueries = await db.SqlQueries
            .Where(value => document.Tasks.Select(item => item.ReferenceQueryId).Contains(value.Id))
            .ToDictionaryAsync(value => value.Id, ct);
        var existingTasks = await db.SqlTasks
            .Where(value => document.Tasks.Select(item => item.Id).Contains(value.Id))
            .ToDictionaryAsync(value => value.Id, ct);

        ValidateExistingOwnership(
            document,
            existingTypes,
            existingDefinitions,
            existingDatabases,
            existingTables,
            existingColumns,
            existingRows,
            existingCells,
            existingParameterValues,
            existingTasks);

        dbms ??= Add(CreateDbms(document.Dbms));
        UpdateDbms(dbms, document.Dbms);

        foreach (var source in types)
        {
            var entity = GetOrAdd(existingTypes, source.Id, () =>
                PhysicalType.Create(document.Dbms.Id, source.TypeName, source.Id));
            entity.Update(source.TypeName);
        }

        foreach (var source in parameterDefinitions)
        {
            var physicalTypeId = types.Single(type => type.Parameters.Contains(source)).Id;
            var entity = GetOrAdd(existingDefinitions, source.Id, () => ParameterDefinition.Create(
                physicalTypeId,
                source.Key,
                source.DisplayName,
                source.InputType,
                source.DefaultValue,
                source.SortOrder,
                source.SqlFragment,
                source.IsRequired,
                source.ValuePrefix,
                source.ValueSuffix,
                source.Separator,
                source.Id));
            entity.Update(
                source.Key,
                source.DisplayName,
                source.InputType,
                source.DefaultValue,
                source.SortOrder,
                source.SqlFragment,
                source.IsRequired,
                source.ValuePrefix,
                source.ValueSuffix,
                source.Separator);
        }

        foreach (var source in databases)
        {
            var entity = GetOrAdd(existingDatabases, source.Id, () => TargetDb.Create(
                source.DbmsId, source.Name, source.Description, source.IsReadOnly, source.Id));
            entity.Update(source.Name, source.Description, source.IsReadOnly);
        }

        foreach (var database in databases)
        {
            foreach (var source in database.Tables)
            {
                var entity = GetOrAdd(existingTables, source.Id, () => MetaTable.Create(
                    database.Id, source.Name, source.Description, source.Id, source.SortOrder));
                entity.Update(source.Name, source.Description, source.SortOrder);
            }
        }

        foreach (var table in tables)
        {
            foreach (var source in table.Columns)
            {
                var entity = GetOrAdd(existingColumns, source.Id, () => MetaAttribute.Create(
                    table.Id,
                    source.PhysicalTypeId,
                    source.Name,
                    source.IsPrimaryKey,
                    source.IsRequired,
                    source.SortOrder,
                    source.Id));
                entity.UpdateSchema(
                    source.Name,
                    source.PhysicalTypeId,
                    source.IsPrimaryKey,
                    source.IsRequired,
                    source.SortOrder);
            }
        }

        foreach (var table in tables)
        {
            foreach (var source in table.Rows)
            {
                var entity = GetOrAdd(existingRows, source.Id, () =>
                    DataRecord.Create(table.Id, source.SortOrder, source.Id));
                entity.Update(source.SortOrder);
            }
        }

        foreach (var source in cells)
        {
            var entity = GetOrAdd(existingCells, source.Id, () => CellValue.Create(
                source.RecordId, source.ColumnId, source.Value, source.Id));
            entity.Update(source.Value);
        }

        foreach (var source in parameterValues)
        {
            var entity = GetOrAdd(existingParameterValues, source.Id, () =>
                AttributeParameterValue.Create(
                    source.ColumnId,
                    source.DefinitionId,
                    source.Value,
                    source.Id));
            entity.Update(source.Value);
        }

        foreach (var source in relationshipPairs)
        {
            var entity = GetOrAdd(existingRelationships, source.Id, () => MetaRelationship.Create(
                source.Name,
                source.FromColumnId,
                source.ToColumnId,
                source.OnDelete,
                source.OnUpdate,
                source.Id));
            entity.UpdateSchema(
                source.Name,
                source.FromColumnId,
                source.ToColumnId,
                source.OnDelete,
                source.OnUpdate);
        }

        foreach (var source in document.Topics)
        {
            var entity = GetOrAdd(existingTopics, source.Id, () => Topic.Create(
                source.Name, source.ParentTopicId, source.Id, source.Description));
            entity.Update(source.Name, source.Description);
            entity.Move(source.ParentTopicId);
        }

        foreach (var source in document.Tasks)
        {
            var reference = source.ReferenceQuery;
            var query = GetOrAdd(existingQueries, source.ReferenceQueryId, () => SqlQuery.Create(
                reference.SqlText,
                reference.StrictColumnOrder,
                reference.StrictRowOrder,
                source.TargetDatabaseId,
                source.ReferenceQueryId));
            query.Update(
                source.TargetDatabaseId,
                reference.SqlText,
                reference.StrictColumnOrder,
                reference.StrictRowOrder);
            query.SetExpectedResult(SerializeExpectedResult(reference.ExpectedResult));

            var status = Enum.Parse<PublicationStatus>(source.PublicationStatus, true);
            var task = GetOrAdd(existingTasks, source.Id, () => SqlTask.Create(
                source.TopicId,
                source.ReferenceQueryId,
                source.Name,
                source.Text,
                source.DifficultyLevel,
                source.Id,
                status));
            task.Update(source.Name, source.Text, source.DifficultyLevel, status);
            task.ChangeTopic(source.TopicId);
        }

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    private T GetOrAdd<T>(Dictionary<Guid, T> existing, Guid id, Func<T> factory)
        where T : class
    {
        if (existing.TryGetValue(id, out var entity))
        {
            return entity;
        }

        entity = Add(factory());
        existing.Add(id, entity);
        return entity;
    }

    private T Add<T>(T entity) where T : class
    {
        db.Add(entity);
        return entity;
    }

    private static DbmsDictionary CreateDbms(CourseSeedDbms source) => DbmsDictionary.Create(
        source.DbmsName,
        source.DbmsSystemName,
        source.DockerImage,
        source.DefaultPort,
        source.EnvUserKey,
        source.EnvPasswordKey,
        source.EnvDatabaseKey,
        source.ExtraEnvConfig,
        source.DefaultDatabase,
        source.DefaultUsername,
        source.DefaultPassword,
        source.Id);

    private static void UpdateDbms(DbmsDictionary entity, CourseSeedDbms source) => entity.Update(
        source.DbmsName,
        source.DbmsSystemName,
        source.DockerImage,
        source.DefaultPort,
        source.EnvUserKey,
        source.EnvPasswordKey,
        source.EnvDatabaseKey,
        source.ExtraEnvConfig,
        source.DefaultDatabase,
        source.DefaultUsername,
        source.DefaultPassword);

    private static string SerializeExpectedResult(CourseSeedExpectedResult source)
    {
        var rows = source.Rows
            .Select(row => (IReadOnlyList<string?>)row)
            .ToList();
        return GoldenResult.Serialize(new QueryResultSet(
            true,
            null,
            source.Columns,
            rows,
            rows.Count,
            0));
    }

    private static List<CourseSeedCell> BuildCells(IEnumerable<CourseSeedTable> tables) => tables
        .SelectMany(table => table.Rows.SelectMany(row =>
            row.Values.Select(value =>
            {
                var column = table.Columns.Single(column => column.Name == value.Key);
                return new CourseSeedCell(
                    CreateStableGuid(row.Id, column.Id.ToString("D")),
                    row.Id,
                    column.Id,
                    value.Value);
            })))
        .ToList();

    private static List<CourseSeedParameterValue> BuildParameterValues(
        IEnumerable<CourseSeedColumn> columns,
        IReadOnlyList<CourseSeedPhysicalType> types) => columns
        .SelectMany(column => column.Parameters.Select(parameter =>
        {
            var definition = types.Single(type => type.Id == column.PhysicalTypeId)
                .Parameters.Single(value => value.Key == parameter.Key);
            return new CourseSeedParameterValue(
                CreateStableGuid(column.Id, definition.Id.ToString("D")),
                column.Id,
                definition.Id,
                parameter.Value);
        }))
        .ToList();

    private static IEnumerable<CourseSeedRelationshipPair> ExpandRelationship(
        CourseSeedRelationship relationship) => relationship.ColumnPairs.Select((pair, index) =>
        new CourseSeedRelationshipPair(
            index == 0
                ? relationship.Id
                : CreateStableGuid(relationship.Id, $"{pair.FromColumnId:D}:{pair.ToColumnId:D}"),
            relationship.ColumnPairs.Count == 1
                ? relationship.Name
                : $"{relationship.Name}_{index + 1}",
            pair.FromColumnId,
            pair.ToColumnId,
            relationship.OnDelete,
            relationship.OnUpdate));

    private static void Validate(CourseSeedDocument document)
    {
        Require(document.FormatVersion == 1, "Поддерживается только formatVersion=1.");
        Require(document.Course.Id != Guid.Empty, "course.id обязателен.");
        Require(!String.IsNullOrWhiteSpace(document.Course.Code), "course.code обязателен.");
        Require(!String.IsNullOrWhiteSpace(document.Course.Name), "course.name обязателен.");
        Require(document.Dbms.Id != Guid.Empty, "dbms.id обязателен.");
        Require(document.Dbms.DefaultPort is > 0 and <= 65535, "dbms.defaultPort должен быть от 1 до 65535.");
        Require(document.Databases.Count > 0, "Курс должен содержать хотя бы одну базу.");
        Require(document.Topics.Count > 0, "Курс должен содержать хотя бы одну тему.");
        Require(document.Tasks.Count > 0, "Курс должен содержать хотя бы одно задание.");

        var allIds = new List<Guid> { document.Course.Id, document.Dbms.Id };
        allIds.AddRange(document.PhysicalTypes.Select(value => value.Id));
        allIds.AddRange(document.PhysicalTypes.SelectMany(value => value.Parameters).Select(value => value.Id));
        allIds.AddRange(document.Databases.Select(value => value.Id));
        allIds.AddRange(document.Databases.SelectMany(value => value.Tables).Select(value => value.Id));
        allIds.AddRange(document.Databases.SelectMany(value => value.Tables)
            .SelectMany(value => value.Columns).Select(value => value.Id));
        allIds.AddRange(document.Databases.SelectMany(value => value.Tables)
            .SelectMany(value => value.Rows).Select(value => value.Id));
        allIds.AddRange(document.Databases.SelectMany(value => value.Relationships).Select(value => value.Id));
        allIds.AddRange(document.Topics.Select(value => value.Id));
        allIds.AddRange(document.Tasks.Select(value => value.Id));
        allIds.AddRange(document.Tasks.Select(value => value.ReferenceQueryId));
        Require(allIds.All(id => id != Guid.Empty), "Все идентификаторы курса должны быть UUID, отличными от empty.");
        Require(allIds.Distinct().Count() == allIds.Count, "Идентификаторы сущностей курса должны быть уникальны.");

        var typeIds = document.PhysicalTypes.Select(value => value.Id).ToHashSet();
        var databaseIds = document.Databases.Select(value => value.Id).ToHashSet();
        var topicIds = document.Topics.Select(value => value.Id).ToHashSet();
        foreach (var database in document.Databases)
        {
            Require(database.DbmsId == document.Dbms.Id, $"База {database.Id:D} ссылается на неизвестную DBMS.");
            Require(database.Tables.Select(value => value.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() ==
                    database.Tables.Count,
                $"В базе {database.Id:D} имена таблиц должны быть уникальны.");
            var tableIds = database.Tables.Select(value => value.Id).ToHashSet();
            var columnsById = database.Tables.SelectMany(value => value.Columns)
                .ToDictionary(value => value.Id);
            foreach (var table in database.Tables)
            {
                Require(table.Columns.Count > 0, $"Таблица {table.Id:D} должна содержать столбцы.");
                Require(table.Columns.Select(value => value.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() ==
                        table.Columns.Count,
                    $"В таблице {table.Id:D} имена столбцов должны быть уникальны.");
                foreach (var column in table.Columns)
                {
                    Require(typeIds.Contains(column.PhysicalTypeId),
                        $"Столбец {column.Id:D} ссылается на неизвестный physicalTypeId.");
                    var definitions = document.PhysicalTypes.Single(type => type.Id == column.PhysicalTypeId)
                        .Parameters.Select(value => value.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
                    Require(column.Parameters.Keys.All(definitions.Contains),
                        $"Столбец {column.Id:D} содержит неизвестный параметр типа.");
                }

                var columnNames = table.Columns.Select(value => value.Name)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var requiredColumns = table.Columns.Where(value => value.IsRequired)
                    .Select(value => value.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
                foreach (var row in table.Rows)
                {
                    Require(row.Values.Keys.All(columnNames.Contains),
                        $"Строка {row.Id:D} содержит неизвестный столбец.");
                    Require(requiredColumns.All(row.Values.ContainsKey),
                        $"Строка {row.Id:D} не содержит обязательный столбец.");
                    Require(row.Values.Where(pair => requiredColumns.Contains(pair.Key)).All(pair => pair.Value is not null),
                        $"Строка {row.Id:D} содержит null в обязательном столбце.");
                }
            }

            foreach (var relationship in database.Relationships)
            {
                Require(tableIds.Contains(relationship.FromTableId) && tableIds.Contains(relationship.ToTableId),
                    $"Связь {relationship.Id:D} ссылается на таблицу другой базы.");
                Require(relationship.ColumnPairs.Count > 0,
                    $"Связь {relationship.Id:D} должна содержать columnPairs.");
                foreach (var pair in relationship.ColumnPairs)
                {
                    Require(columnsById.TryGetValue(pair.FromColumnId, out var fromColumn),
                        $"Связь {relationship.Id:D} ссылается на неизвестный столбец.");
                    Require(columnsById.TryGetValue(pair.ToColumnId, out var toColumn),
                        $"Связь {relationship.Id:D} ссылается на неизвестный столбец.");
                    Require(database.Tables.Single(table => table.Id == relationship.FromTableId)
                            .Columns.Contains(fromColumn!) &&
                            database.Tables.Single(table => table.Id == relationship.ToTableId)
                            .Columns.Contains(toColumn!),
                        $"Связь {relationship.Id:D} содержит столбец не из указанной таблицы.");
                }
            }
        }

        foreach (var topic in document.Topics)
        {
            Require(!topic.ParentTopicId.HasValue || topicIds.Contains(topic.ParentTopicId.Value),
                $"Тема {topic.Id:D} ссылается на неизвестную родительскую тему.");
            Require(topic.ParentTopicId != topic.Id, $"Тема {topic.Id:D} не может быть родителем самой себе.");
        }

        foreach (var task in document.Tasks)
        {
            Require(topicIds.Contains(task.TopicId), $"Задание {task.Id:D} ссылается на неизвестную тему.");
            Require(databaseIds.Contains(task.TargetDatabaseId),
                $"Задание {task.Id:D} ссылается на неизвестную базу.");
            Require(task.DifficultyLevel is >= 1 and <= 5,
                $"Сложность задания {task.Id:D} должна быть от 1 до 5.");
            Require(Enum.TryParse<PublicationStatus>(task.PublicationStatus, true, out _),
                $"Задание {task.Id:D} содержит неизвестный publicationStatus.");
            Require(!String.IsNullOrWhiteSpace(task.ReferenceQuery.SqlText),
                $"Задание {task.Id:D} не содержит эталонный SQL.");
            var width = task.ReferenceQuery.ExpectedResult.Columns.Count;
            Require(task.ReferenceQuery.ExpectedResult.Rows.All(row => row.Count == width),
                $"Задание {task.Id:D} содержит строку результата неверной ширины.");
        }
    }

    private static void ValidateExistingOwnership(
        CourseSeedDocument document,
        IReadOnlyDictionary<Guid, PhysicalType> types,
        IReadOnlyDictionary<Guid, ParameterDefinition> definitions,
        IReadOnlyDictionary<Guid, TargetDb> databases,
        IReadOnlyDictionary<Guid, MetaTable> tables,
        IReadOnlyDictionary<Guid, MetaAttribute> columns,
        IReadOnlyDictionary<Guid, DataRecord> rows,
        IReadOnlyDictionary<Guid, CellValue> cells,
        IReadOnlyDictionary<Guid, AttributeParameterValue> parameterValues,
        IReadOnlyDictionary<Guid, SqlTask> tasks)
    {
        foreach (var source in document.PhysicalTypes)
        {
            RequireOwner(types.GetValueOrDefault(source.Id)?.DbmsId, document.Dbms.Id, nameof(PhysicalType), source.Id);
            foreach (var parameter in source.Parameters)
            {
                RequireOwner(definitions.GetValueOrDefault(parameter.Id)?.PhysicalTypeId,
                    source.Id, nameof(ParameterDefinition), parameter.Id);
            }
        }

        foreach (var database in document.Databases)
        {
            RequireOwner(databases.GetValueOrDefault(database.Id)?.DbmsId,
                document.Dbms.Id, nameof(TargetDb), database.Id);
            foreach (var table in database.Tables)
            {
                RequireOwner(tables.GetValueOrDefault(table.Id)?.TargetDbId,
                    database.Id, nameof(MetaTable), table.Id);
                foreach (var column in table.Columns)
                {
                    RequireOwner(columns.GetValueOrDefault(column.Id)?.MetaTableId,
                        table.Id, nameof(MetaAttribute), column.Id);
                }

                foreach (var row in table.Rows)
                {
                    RequireOwner(rows.GetValueOrDefault(row.Id)?.MetaTableId,
                        table.Id, nameof(DataRecord), row.Id);
                    foreach (var value in row.Values)
                    {
                        var column = table.Columns.Single(item => item.Name == value.Key);
                        var cellId = CreateStableGuid(row.Id, column.Id.ToString("D"));
                        var cell = cells.GetValueOrDefault(cellId);
                        RequireOwner(cell?.DataRecordId, row.Id, nameof(CellValue), cellId);
                        RequireOwner(cell?.MetaAttributeId, column.Id, nameof(CellValue), cellId);
                    }
                }

                foreach (var column in table.Columns)
                {
                    var type = document.PhysicalTypes.Single(item => item.Id == column.PhysicalTypeId);
                    foreach (var parameter in column.Parameters)
                    {
                        var definition = type.Parameters.Single(item => item.Key == parameter.Key);
                        var valueId = CreateStableGuid(column.Id, definition.Id.ToString("D"));
                        var existing = parameterValues.GetValueOrDefault(valueId);
                        RequireOwner(existing?.MetaAttributeId, column.Id,
                            nameof(AttributeParameterValue), valueId);
                        RequireOwner(existing?.ParameterDefinitionId, definition.Id,
                            nameof(AttributeParameterValue), valueId);
                    }
                }
            }
        }

        foreach (var source in document.Tasks)
        {
            RequireOwner(tasks.GetValueOrDefault(source.Id)?.SqlQueryId,
                source.ReferenceQueryId, nameof(SqlTask), source.Id);
        }
    }

    private static void RequireOwner(Guid? actual, Guid expected, string entityType, Guid entityId)
    {
        if (actual.HasValue && actual != expected)
        {
            throw new InvalidOperationException(
                $"Course seed не может использовать {entityType} {entityId:D}: запись принадлежит другому набору данных.");
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"Некорректный CourseSeed JSON: {message}");
        }
    }

    private static Guid CreateStableGuid(Guid namespaceId, string value)
    {
        var namespaceBytes = namespaceId.ToByteArray();
        var valueBytes = Encoding.UTF8.GetBytes(value);
        var source = new byte[namespaceBytes.Length + valueBytes.Length];
        namespaceBytes.CopyTo(source, 0);
        valueBytes.CopyTo(source, namespaceBytes.Length);
        var hash = SHA256.HashData(source);
        return new Guid(hash.AsSpan(0, 16));
    }

    private sealed record CourseSeedCell(Guid Id, Guid RecordId, Guid ColumnId, string? Value);

    private sealed record CourseSeedParameterValue(
        Guid Id,
        Guid ColumnId,
        Guid DefinitionId,
        string Value);

    private sealed record CourseSeedRelationshipPair(
        Guid Id,
        string Name,
        Guid FromColumnId,
        Guid ToColumnId,
        string? OnDelete,
        string? OnUpdate);
}
