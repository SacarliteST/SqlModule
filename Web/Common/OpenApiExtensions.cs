using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using SQLModule.Contracts;

namespace SQLModule.Web.Common;

internal static class OpenApiExtensions
{
    private const string BearerScheme = "Bearer";

    internal static IServiceCollection AddOpenApiDocumentation(this IServiceCollection services) =>
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "SQLModule API",
                Version = "v1",
                Description = "API SQL-тренажёра (модуль Scoodle)."
            });

            c.SupportNonNullableReferenceTypes();
            c.UseAllOfToExtendReferenceSchemas();
            c.SchemaFilter<ApiProblemSchemaFilter>();
            c.SchemaFilter<StringEnumSchemaFilter>();
            c.SchemaFilter<NullableResultRowsSchemaFilter>();
            c.OperationFilter<SubmitAttemptIdempotencyOpenApiFilter>();
            c.OperationFilter<AttemptFilterOptionsOpenApiFilter>();
            c.OperationFilter<LookupValuesOpenApiFilter>();
            c.OperationFilter<ModuleIntegrationServiceKeyOpenApiFilter>();
            c.OperationFilter<IntegrationErrorCodesOpenApiFilter>();
            c.AddSecurityDefinition(BearerScheme, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Введите access token, полученный от IdentityService."
            });
            c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(BearerScheme, document, null)] = []
            });

            foreach (var assembly in new[] { typeof(IWebMarker).Assembly, typeof(ApiRoutes).Assembly })
            {
                var xml = Path.Combine(AppContext.BaseDirectory, $"{assembly.GetName().Name}.xml");
                if (File.Exists(xml))
                {
                    c.IncludeXmlComments(xml);
                }
            }
        });
}
