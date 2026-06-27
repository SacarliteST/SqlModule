using Microsoft.Extensions.DependencyInjection;
using SQLModule.Sandbox;
using SQLModule.Web.Common.Sandbox;
using SQLModule.Web.Features.DbmsCatalog.DbmsDictionary;
using SQLModule.Web.Features.DbmsCatalog.ParameterDefinitions;
using SQLModule.Web.Features.DbmsCatalog.PhysicalTypes;
using SQLModule.Web.Features.Schema.AttributeParameterValues;
using SQLModule.Web.Features.Schema.CellValues;
using SQLModule.Web.Features.Schema.DataRecords;
using SQLModule.Web.Features.Schema.MetaAttributes;
using SQLModule.Web.Features.Schema.MetaRelationships;
using SQLModule.Web.Features.Schema.MetaTables;
using SQLModule.Web.Features.Schema.SchemaBuilder;
using SQLModule.Web.Features.Schema.TargetDbs;
using SQLModule.Web.Features.Training.Attempts;
using SQLModule.Web.Features.Training.SqlQueries;
using SQLModule.Web.Features.Training.SqlTasks;
using SQLModule.Web.Features.Training.Topics;

namespace SQLModule.Web.Common;

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
        => s.AddSandbox()
            .AddTaskMaterializer()
            .AddDbmsDictionaries()
            .AddPhysicalTypes()
            .AddParameterDefinitions()
            .AddTargetDbs()
            .AddMetaTables()
            .AddMetaAttributes()
            .AddMetaRelationships()
            .AddDataRecords()
            .AddCellValues()
            .AddAttributeParameterValues()
            .AddTopics()
            .AddSqlTasks()
            .AddSqlQueries()
            .AddAttempts()
            .AddSchemaBuilder();
}
