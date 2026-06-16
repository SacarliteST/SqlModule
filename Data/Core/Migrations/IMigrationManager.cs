namespace TemplateApi.Data.Core.Migrations;

/// <summary>
/// Менеджер миграций БД
/// </summary>
public interface IMigrationManager
{
    /// <summary>
    /// Провести миграции БД
    /// </summary>
    public Task MigrateAsync();
}
