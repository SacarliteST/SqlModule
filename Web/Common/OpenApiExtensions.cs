using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using SQLModule.Contracts;

namespace SQLModule.Web.Common;

internal static class OpenApiExtensions
{
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
