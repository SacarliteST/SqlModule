using SQLModule.Contracts.Schema.SchemaBuilder;

namespace SQLModule.Client.SchemaBuilder;

/// <summary>Клиент для работы с построителем схемы.</summary>
public interface ISchemaBuilderClient
{
    /// <summary>Проверяет черновик схемы в реальном Docker-контейнере без сохранения.</summary>
    Task ValidateAsync(CreateSchemaRequest request, CancellationToken ct = default);
}
