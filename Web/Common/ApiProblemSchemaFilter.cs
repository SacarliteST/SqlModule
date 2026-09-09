using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SQLModule.Web.Common;

internal sealed class ApiProblemSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (!typeof(ProblemDetails).IsAssignableFrom(context.Type))
        {
            return;
        }

        if (schema.Properties is null)
        {
            return;
        }

        schema.Properties["code"] = new OpenApiSchema
        {
            Type = JsonSchemaType.String
        };
        schema.Properties["traceId"] = new OpenApiSchema
        {
            Type = JsonSchemaType.String
        };
        schema.Properties["errors"] = new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            AdditionalProperties = new OpenApiSchema
            {
                Type = JsonSchemaType.Array,
                Items = new OpenApiSchema
                {
                    Type = JsonSchemaType.String
                }
            }
        };
        schema.Properties["violations"] = new OpenApiSchema
        {
            Type = JsonSchemaType.Array,
            Items = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["path"] = new OpenApiSchema { Type = JsonSchemaType.String },
                    ["code"] = new OpenApiSchema { Type = JsonSchemaType.String },
                    ["message"] = new OpenApiSchema { Type = JsonSchemaType.String },
                    ["severity"] = new OpenApiSchema { Type = JsonSchemaType.String },
                    ["affectedRows"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Integer,
                        Format = "int64"
                    }
                }
            }
        };
    }
}
