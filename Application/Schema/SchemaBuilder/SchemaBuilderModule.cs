using Microsoft.Extensions.DependencyInjection;
using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Schema.SchemaBuilder.ApplyTargetDbSchema;
using SQLModule.Application.Schema.SchemaBuilder.BatchTableRows;
using SQLModule.Application.Schema.SchemaBuilder.CreateSchema;
using SQLModule.Application.Schema.SchemaBuilder.CreateTargetDbFromDdl;
using SQLModule.Application.Schema.SchemaBuilder.GetLookupValues;
using SQLModule.Application.Schema.SchemaBuilder.GetTableRows;
using SQLModule.Application.Schema.SchemaBuilder.GetTargetDbSchema;
using SQLModule.Application.Schema.SchemaBuilder.ValidateSchema;
using SQLModule.Application.Schema.SchemaBuilder.ValidateTargetDbDdl;
using SQLModule.Application.Schema.SchemaBuilder.ValidateTargetDbSchema;

namespace SQLModule.Application.Schema.SchemaBuilder;

public static class SchemaBuilderModule
{
    public static IServiceCollection AddSchemaBuilder(this IServiceCollection services)
    {
        services.AddScoped<ISchemaPreparer, SchemaPreparer>();
        services.AddScoped<ISchemaSnapshotReader, SchemaSnapshotReader>();
        services.AddScoped<ISchemaDiffPlanner, SchemaDiffPlanner>();
        services.AddScoped<ISchemaDiffApplier, SchemaDiffApplier>();
        services.AddScoped<IDdlSchemaService, DdlSchemaService>();
        services.AddScoped<IRequestHandler<GetTargetDbSchemaQuery, Result<TargetDbSchemaResponse>>, GetTargetDbSchemaHandler>();
        services.AddScoped<IRequestHandler<ValidateTargetDbSchemaCommand, Result<SchemaValidationResponse>>, ValidateTargetDbSchemaHandler>();
        services.AddScoped<IRequestHandler<ApplyTargetDbSchemaCommand, Result<TargetDbSchemaResponse>>, ApplyTargetDbSchemaHandler>();
        services.AddScoped<IRequestHandler<GetTableRowsQuery, Result<TableRowsResponse>>, GetTableRowsHandler>();
        services.AddScoped<IRequestHandler<GetLookupValuesQuery, Result<LookupValuesResponse>>, GetLookupValuesHandler>();
        services.AddScoped<IRequestHandler<BatchTableRowsCommand, Result<BatchTableRowsResponse>>, BatchTableRowsHandler>();
        services.AddScoped<IRequestHandler<ValidateTargetDbDdlCommand, Result<ValidateTargetDbDdlResponse>>, ValidateTargetDbDdlHandler>();
        services.AddScoped<IRequestHandler<CreateTargetDbFromDdlCommand, Result<CreateTargetDbFromDdlResponse>>, CreateTargetDbFromDdlHandler>();
        services.AddScoped<IRequestHandler<ValidateSchemaCommand, Result>, ValidateSchemaHandler>();
        services.AddScoped<IRequestHandler<CreateSchemaCommand, Result<CreateSchemaResponse>>, CreateSchemaHandler>();
        return services;
    }
}
