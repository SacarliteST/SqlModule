namespace SQLModule.Sandbox;

/// <summary>Генерирует SQL-инструкции DDL и INSERT по нейтральным моделям схемы и данных.</summary>
public interface ISchemaSqlGenerator
{
    /// <summary>
    /// Генерирует инструкции DDL: <c>CREATE TABLE</c> для каждой таблицы
    /// и <c>ALTER TABLE ADD CONSTRAINT</c> для каждой связи.
    /// </summary>
    IReadOnlyList<string> GenerateDdl(ISqlSyntax syntax, SchemaSpec schema);

    /// <summary>
    /// Генерирует <c>INSERT</c>-инструкции для всех строк данных.
    /// Порядок таблиц определяется топологической сортировкой по Foreign Key (родители вставляются первыми).
    /// </summary>
    IReadOnlyList<string> GenerateInserts(ISqlSyntax syntax, SchemaSpec schema, DataSpec data);
}
