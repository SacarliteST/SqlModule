using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Features.Schema.SchemaBuilder.CreateSchema;
using SQLModule.Host.Features.Schema.SchemaBuilder.ValidateSchema;

namespace SQLModule.Host.Features.Schema.SchemaBuilder;

internal static class SchemaBuilderModule
{
    internal static IServiceCollection AddSchemaBuilder(this IServiceCollection services)
    {
        services.AddScoped<ISchemaPreparer, SchemaPreparer>();
        services.AddScoped<IRequestHandler<ValidateSchemaCommand, Result>, ValidateSchemaHandler>();
        services.AddScoped<IRequestHandler<CreateSchemaCommand, Result<CreateSchemaResponse>>, CreateSchemaHandler>();
        return services;
    }
}
