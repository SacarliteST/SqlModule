using SQLModule.Contracts.Schema.SchemaBuilder;

namespace SQLModule.Client.SchemaBuilder;

/// <summary>Клиент для работы с построителем схемы.</summary>
public interface ISchemaBuilderClient
{
    /// <summary>Проверяет черновик схемы в реальном Docker-контейнере без сохранения.</summary>
    Task ValidateAsync(CreateSchemaRequest request, CancellationToken ct = default);

    /// <summary>Создаёт схему: валидирует DDL и атомарно сохраняет мету. Возвращает карту TempId → реальных Id.</summary>
    Task<CreateSchemaResponse> CreateAsync(CreateSchemaRequest request, CancellationToken ct = default);
}
