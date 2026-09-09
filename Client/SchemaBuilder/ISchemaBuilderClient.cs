using SQLModule.Contracts.Schema.SchemaBuilder;

namespace SQLModule.Client.SchemaBuilder;

/// <summary>Клиент для работы с построителем схемы.</summary>
public interface ISchemaBuilderClient
{
    /// <summary>Проверяет черновик схемы в реальном Docker-контейнере без сохранения.</summary>
    Task ValidateAsync(CreateSchemaRequest request, CancellationToken ct = default);

    /// <summary>Создаёт схему: валидирует DDL и атомарно сохраняет мету. Возвращает карту TempId → реальных Id.</summary>
    Task<CreateSchemaResponse> CreateAsync(CreateSchemaRequest request, CancellationToken ct = default);

    /// <summary>Получить агрегированный снимок схемы существующей учебной базы.</summary>
    Task<TargetDbSchemaResponse?> GetTargetDbSchemaAsync(Guid targetDbId, CancellationToken ct = default);
    /// <summary>Проверить полный желаемый снимок без сохранения.</summary>
    Task<SchemaValidationResponse> ValidateTargetDbSchemaAsync(
        Guid targetDbId, SchemaUpsertRequest request, CancellationToken ct = default);
    /// <summary>Атомарно проверить и применить полный снимок схемы.</summary>
    Task<TargetDbSchemaResponse> ApplyTargetDbSchemaAsync(
        Guid targetDbId, SchemaUpsertRequest request, string? idempotencyKey = null,
        CancellationToken ct = default);
    /// <summary>Получить страницу учебных строк таблицы.</summary>
    Task<TableRowsResponse?> GetTableRowsAsync(
        Guid targetDbId, Guid tableId, int offset, int limit, CancellationToken ct = default);
    /// <summary>Атомарно сохранить пакет изменений строк.</summary>
    Task<BatchTableRowsResponse> SaveTableRowsAsync(
        Guid targetDbId, Guid tableId, string idempotencyKey,
        BatchTableRowsRequest request, CancellationToken ct = default);

    /// <summary>Проверить пользовательский DDL без сохранения.</summary>
    Task<ValidateTargetDbDdlResponse> ValidateTargetDbDdlAsync(
        ValidateTargetDbDdlRequest request, CancellationToken ct = default);

    /// <summary>Атомарно создать учебную базу из пользовательского DDL.</summary>
    Task<CreateTargetDbFromDdlResponse> CreateTargetDbFromDdlAsync(
        string idempotencyKey, CreateTargetDbFromDdlRequest request, CancellationToken ct = default);
}
