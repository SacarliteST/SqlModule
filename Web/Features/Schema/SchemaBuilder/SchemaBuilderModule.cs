using Microsoft.Extensions.DependencyInjection;
using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Web.Common.Cqrs;
using SQLModule.Web.Features.Schema.SchemaBuilder.ApplyTargetDbSchema;
using SQLModule.Web.Features.Schema.SchemaBuilder.BatchTableRows;
using SQLModule.Web.Features.Schema.SchemaBuilder.CreateSchema;
using SQLModule.Web.Features.Schema.SchemaBuilder.CreateTargetDbFromDdl;
using SQLModule.Web.Features.Schema.SchemaBuilder.GetTableRows;
using SQLModule.Web.Features.Schema.SchemaBuilder.GetLookupValues;
using SQLModule.Web.Features.Schema.SchemaBuilder.GetTargetDbSchema;
using SQLModule.Web.Features.Schema.SchemaBuilder.ValidateSchema;
using SQLModule.Web.Features.Schema.SchemaBuilder.ValidateTargetDbDdl;
using SQLModule.Web.Features.Schema.SchemaBuilder.ValidateTargetDbSchema;

namespace SQLModule.Web.Features.Schema.SchemaBuilder;

internal static class SchemaBuilderModule
{
    internal static IServiceCollection AddSchemaBuilder(this IServiceCollection services)
    {
        services.AddScoped<ISchemaPreparer, SchemaPreparer>();
        services.AddScoped<ISchemaSnapshotReader, SchemaSnapshotReader>();
        services.AddScoped<ISchemaDiffPlanner, SchemaDiffPlanner>();
        services.AddScoped<ISchemaDiffApplier, SchemaDiffApplier>();
        services.AddScoped<IDdlSchemaService, DdlSchemaService>();
        services.AddScoped<FluentValidation.IValidator<SchemaUpsertRequest>, SchemaUpsertValidator>();
        services.AddScoped<FluentValidation.IValidator<BatchTableRowsRequest>, BatchTableRowsValidator>();
        services.AddScoped<FluentValidation.IValidator<LookupValuesRequest>, LookupValuesRequestValidator>();
        services.AddScoped<FluentValidation.IValidator<ValidateTargetDbDdlRequest>, ValidateTargetDbDdlRequestValidator>();
        services.AddScoped<FluentValidation.IValidator<CreateTargetDbFromDdlRequest>, CreateTargetDbFromDdlRequestValidator>();
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
