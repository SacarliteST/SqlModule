using SQLModule.Host.Features.DbmsCatalog.DbmsDictionary;
using SQLModule.Host.Features.DbmsCatalog.ParameterDefinitions;
using SQLModule.Host.Features.DbmsCatalog.PhysicalTypes;
using SQLModule.Host.Features.Schema.AttributeParameterValues;
using SQLModule.Host.Features.Schema.DataRecords;
using SQLModule.Host.Features.Schema.MetaAttributes;
using SQLModule.Host.Features.Schema.MetaRelationships;
using SQLModule.Host.Features.Schema.MetaTables;
using SQLModule.Host.Features.Schema.TargetDbs;
using SQLModule.Host.Features.Training.Attempts;
using SQLModule.Host.Features.Training.SqlQueries;
using SQLModule.Host.Features.Training.SqlTasks;
using SQLModule.Host.Features.Training.Topics;

namespace SQLModule.Host.Common;

/// <summary>
/// Единый агрегатор регистрации всех фич приложения.
/// Тест-сканер HandlerScannerTests использует этот метод, поэтому пропуск регистрации
/// в AddFeatures() автоматически делает тест красным.
/// </summary>
/// <remarks>
/// Правило: каждый новый <c>XxxModule.AddXxx()</c> добавляется ТОЛЬКО сюда, одной строкой.
/// </remarks>
internal static class FeaturesExtensions
{
    /// <summary>Регистрирует хендлеры всех фич.</summary>
    internal static IServiceCollection AddFeatures(this IServiceCollection s)
        => s.AddDbmsDictionaries()
            .AddPhysicalTypes()
            .AddParameterDefinitions()
            .AddTargetDbs()
            .AddMetaTables()
            .AddMetaAttributes()
            .AddMetaRelationships()
            .AddDataRecords()
            .AddAttributeParameterValues()
            .AddTopics()
            .AddSqlTasks()
            .AddSqlQueries()
            .AddAttempts();
}
