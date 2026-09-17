using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SQLModule.Web.Common;

/// <summary>Уточняет обязательность и wire-имена query-параметров lookup endpoint.</summary>
internal sealed class LookupValuesOpenApiFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.ApiDescription.ActionDescriptor.EndpointMetadata
            .OfType<EndpointNameMetadata>()
            .All(metadata => metadata.EndpointName != "GetTargetDbTableLookupValues"))
        {
            return;
        }

        foreach (var parameter in operation.Parameters?.OfType<OpenApiParameter>() ?? [])
        {
            parameter.Name = parameter.Name switch
            {
                "ValueColumnId" => "valueColumnId",
                "LabelColumnId" => "labelColumnId",
                "Search" => "search",
                "Offset" => "offset",
                "Limit" => "limit",
                _ => parameter.Name
            };
            parameter.Required = parameter.Name == "valueColumnId" || parameter.In == ParameterLocation.Path;

            if (parameter.Schema is not OpenApiSchema schema)
            {
                continue;
            }

            switch (parameter.Name)
            {
                case "search":
                    schema.MaxLength = 200;
                    break;
                case "offset":
                    schema.Minimum = "0";
                    schema.Default = JsonValue.Create(0);
                    break;
                case "limit":
                    schema.Minimum = "1";
                    schema.Maximum = "100";
                    schema.Default = JsonValue.Create(30);
                    break;
            }
        }
    }
}
