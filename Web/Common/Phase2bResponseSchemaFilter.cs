using Microsoft.OpenApi;
using SQLModule.Contracts.DbmsCatalog.Validation;
using SQLModule.Contracts.Training.Attempt;
using SQLModule.Contracts.Training.Student;
using SQLModule.Contracts.Training.Validation;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SQLModule.Web.Common;

/// <summary>Уточняет обязательность сериализуемых полей и nullable enum в ответах Phase 2b.</summary>
internal sealed class Phase2bResponseSchemaFilter : ISchemaFilter
{
    private static readonly HashSet<Type> ResponseTypes =
    [
        typeof(DbmsValidationCapabilitiesResponse),
        typeof(StudentTaskProgressResponse),
        typeof(StudentTaskValidationResponse),
        typeof(StudentTaskHintsResponse),
        typeof(StudentHintTableResponse),
        typeof(AttemptScoringResponse),
        typeof(AttemptCheckResultResponse),
        typeof(AttemptHintResponse),
        typeof(ProgressFinalizationResponse),
        typeof(SubmitAttemptResponse),
        typeof(StudentTaskDetailsResponse),
        typeof(StudentExecutionLimitsResponse),
        typeof(StudentAttemptListItemResponse),
        typeof(StudentAttemptResponse),
        typeof(AttemptListItemResponse),
        typeof(AttemptResponse),
        typeof(TaskValidationConfigurationResponse),
        typeof(ValidationCheckResponse),
        typeof(TaskValidationPreviewResponse),
        typeof(ValidationCheckPreviewResponse),
        typeof(ValidationViolationResponse)
    ];

    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (!ResponseTypes.Contains(context.Type) || schema is not OpenApiSchema response ||
            response.Properties is null)
        {
            return;
        }

        response.Required ??= new HashSet<string>();
        foreach (var property in response.Properties.Keys)
        {
            response.Required.Add(property);
        }

        if (context.Type == typeof(StudentTaskProgressResponse) &&
            response.Properties.TryGetValue("finalizationReason", out var propertySchema) &&
            propertySchema is OpenApiSchema finalizationReason)
        {
            finalizationReason.Type = JsonSchemaType.String | JsonSchemaType.Null;
        }
    }
}
