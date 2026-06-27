using Microsoft.Extensions.DependencyInjection;
using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Web.Common.Cqrs;
using SQLModule.Web.Features.Schema.SchemaBuilder.CreateSchema;
using SQLModule.Web.Features.Schema.SchemaBuilder.ValidateSchema;

namespace SQLModule.Web.Features.Schema.SchemaBuilder;

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
