using SQLModule.Application.Schema.SchemaBuilder;
using SQLModule.Application.Training.Progress;
using SQLModule.Application.Schema.MetaRelationships;
using SQLModule.Application.Training.Topics;
using SQLModule.Application.Schema.AttributeParameterValues;
using SQLModule.Application.Schema.MetaAttributes;
using SQLModule.Application.Schema.TargetDbs;
using SQLModule.Application.DbmsCatalog.DbmsDictionary;
using SQLModule.Application.Schema.CellValues;
using SQLModule.Application.Schema.DataRecords;
using SQLModule.Application.DbmsCatalog.ParameterDefinitions;
using SQLModule.Application.DbmsCatalog.PhysicalTypes;
using SQLModule.Application.Schema.MetaTables;
﻿using Microsoft.Extensions.DependencyInjection;
using SQLModule.Application.Training.Validation;
using SQLModule.Application.Training.SqlQueries;
using SQLModule.Application.Training.SqlTasks;
using SQLModule.Application.TaskMaterialization;
using SQLModule.Sandbox.Pooling;
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
using SQLModule.Web.Features.Training.Progress;
using SQLModule.Web.Features.Training.SqlQueries;
using SQLModule.Web.Features.Training.SqlTasks;
using SQLModule.Web.Features.Training.Topics;
using SQLModule.Web.Features.Training.Validation;

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
        => s.AddScoped<ISandboxPoolProfileSource, DbmsSandboxPoolProfileSource>()
            .AddAttempts();
}
