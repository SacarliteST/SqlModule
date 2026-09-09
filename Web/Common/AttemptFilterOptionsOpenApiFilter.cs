using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SQLModule.Web.Common;

/// <summary>Оставляет собственную валидацию UUID с 422, но описывает параметры как uuid в OpenAPI.</summary>
internal sealed class AttemptFilterOptionsOpenApiFilter : IOperationFilter
{
    private static readonly HashSet<string> OperationNames =
    [
        "GetAttemptStudentFilterOptions",
        "GetAttemptTopicFilterOptions",
        "GetAttemptTaskFilterOptions"
    ];

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var endpointName = context.ApiDescription.ActionDescriptor.EndpointMetadata
            .OfType<EndpointNameMetadata>()
            .Select(x => x.EndpointName)
            .FirstOrDefault();
        if (endpointName is null || !OperationNames.Contains(endpointName))
        {
            return;
        }

        foreach (var parameter in operation.Parameters?.OfType<OpenApiParameter>() ?? [])
        {
            if ((String.Equals(parameter.Name, "id", StringComparison.OrdinalIgnoreCase) ||
                 String.Equals(parameter.Name, "topicId", StringComparison.OrdinalIgnoreCase)) &&
                parameter.Schema is OpenApiSchema schema)
            {
                schema.Format = "uuid";
            }
        }
    }
}
