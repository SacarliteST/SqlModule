using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SQLModule.Web.Common;

/// <summary>Описывает обязательный service-key у сервер-сервер endpoints интеграции.</summary>
internal sealed class ModuleIntegrationServiceKeyOpenApiFilter : IOperationFilter
{
    private static readonly HashSet<string> OperationNames =
    [
        "GetModuleTasksCatalog",
        "UpsertModuleSession"
    ];

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var endpointName = context.ApiDescription.ActionDescriptor.EndpointMetadata
            .OfType<EndpointNameMetadata>()
            .Select(metadata => metadata.EndpointName)
            .FirstOrDefault();
        if (endpointName is null || !OperationNames.Contains(endpointName))
        {
            return;
        }

        var parameter = operation.Parameters?.OfType<OpenApiParameter>()
            .SingleOrDefault(item => item.Name == "X-Service-Key");
        if (parameter is null)
        {
            return;
        }

        parameter.Required = true;
        parameter.Description = "Секрет сервер-сервер интеграции Education и SqlModule.";
    }
}
