using Microsoft.OpenApi;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Domain.Training;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SQLModule.Web.Common;

internal sealed class StringEnumSchemaFilter : ISchemaFilter
{
    private static readonly HashSet<Type> PublicStringEnums =
    [
        typeof(ExecutionStatus),
        typeof(CheckReason),
        typeof(PublicationStatus),
        typeof(AttemptResultSnapshotState),
        typeof(SchemaLifecycleState),
        typeof(TableRowOperation)
    ];

    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        var type = Nullable.GetUnderlyingType(context.Type) ?? context.Type;
        if (!PublicStringEnums.Contains(type))
        {
            return;
        }

        if (schema is not OpenApiSchema mutableSchema)
        {
            return;
        }

        mutableSchema.Type = JsonSchemaType.String;
        mutableSchema.Format = null;
        mutableSchema.Enum = Enum.GetNames(type)
            .Select(name => (System.Text.Json.Nodes.JsonNode)System.Text.Json.Nodes.JsonValue.Create(name)!)
            .ToList();
    }
}
