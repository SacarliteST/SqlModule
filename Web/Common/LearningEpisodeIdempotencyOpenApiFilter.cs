using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SQLModule.Web.Common;

internal sealed class LearningEpisodeIdempotencyOpenApiFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.ApiDescription.ActionDescriptor.EndpointMetadata
            .OfType<EndpointNameMetadata>()
            .All(metadata => metadata.EndpointName != "FinalizeLearningEpisode"))
        {
            return;
        }

        operation.Parameters ??= [];
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "Idempotency-Key",
            In = ParameterLocation.Header,
            Required = true,
            Description = "UUID операции финализации в формате D.",
            Schema = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Format = "uuid"
            }
        });
    }
}
