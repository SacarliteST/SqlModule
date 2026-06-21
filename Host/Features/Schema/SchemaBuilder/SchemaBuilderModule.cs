using SQLModule.Common.Results;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Features.Schema.SchemaBuilder.ValidateSchema;

namespace SQLModule.Host.Features.Schema.SchemaBuilder;

internal static class SchemaBuilderModule
{
    internal static IServiceCollection AddSchemaBuilder(this IServiceCollection services)
    {
        services.AddScoped<IRequestHandler<ValidateSchemaCommand, Result>, ValidateSchemaHandler>();
        return services;
    }
}
